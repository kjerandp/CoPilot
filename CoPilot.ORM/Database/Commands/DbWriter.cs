using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using CoPilot.ORM.Common;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database.Commands
{
    public class DbWriter : UnitOfWork
    {
        public OperationType Operations { get; set; }
        private readonly Dictionary<object,object> _entities = new Dictionary<object, object>();
        private readonly DbModel _model;
        public ScriptOptions Options;

        public DbWriter(IDb db) : base(db.Connection)
        {
            _model = db.Model;
            Options = ScriptOptions.Default();

            Operations = (OperationType.Insert | OperationType.Update | OperationType.Delete);

            Command.CommandType = CommandType.Text;
        }

        internal DbWriter(DbModel model, SqlConnection connection, ScriptOptions options = null) : base(connection)
        {
            _model = model;
            Options = options ?? ScriptOptions.Default();

            Operations = (OperationType.Insert | OperationType.Update | OperationType.Delete);

            Command.CommandType = CommandType.Text;
        }

        public int ExecuteCommand(string commandText, object args = null)
        {
            var request = DbRequest.CreateRequest(_model, commandText, args);
            return CommandExecutor.ExecuteNonQuery(Command, request);
        }

        public object Scalar(string commandText, object args = null)
        {
            var request = DbRequest.CreateRequest(_model, commandText, args);
            return CommandExecutor.ExecuteScalar(Command, request);
        }

        public T Scalar<T>(string commandText, object args = null)
        {
            object convertedValue;
            ReflectionHelper.ConvertValueToType(typeof(T), Scalar(commandText, args), out convertedValue);

            return (T)convertedValue;
        }

        public void Save<T>(T entity, params string[] include) where T : class
        {
            if (typeof(T).IsCollection())
            {
                throw new ArgumentException("To save all entities in a collection you need to explicitly provide the entity type as a generic argument to the save method.");
            }
            var context = _model.CreateContext<T>(include);
            SaveNode(context, entity);
        }

        public void Save<T>(IEnumerable<T> entities, params string[] include) where T : class
        {
            var context = _model.CreateContext<T>(include);
            foreach (var entity in entities)
            {
                SaveNode(context, entity);
            }
            
        }

        public void Insert<T>(T entity, params string[] include) where T : class
        {
            if (typeof(T).IsCollection())
            {
                throw new ArgumentException("To insert all entities in a collection you need to explicitly provide the entity type as a generic argument to the insert method.");
            }
            var context = _model.CreateContext<T>(include);
            InsertNode(context, entity);
        }

        public void Insert<T>(IEnumerable<T> entities, params string[] include) where T : class
        {
            var context = _model.CreateContext<T>(include);
            foreach (var entity in entities)
            {
                InsertNode(context, entity);
            }
        }

        public void Update<T>(T entity, params string[] include) where T : class
        {
            if (typeof(T).IsCollection())
            {
                throw new ArgumentException("To update all entities in a collection you need to explicitly provide the entity type as a generic argument to the update method.");
            }
            var context = _model.CreateContext<T>(include);
            UpdateNode(context, entity);
        }

        public void Update<T>(IEnumerable<T> entities, params string[] include) where T : class
        {
            var context = _model.CreateContext<T>(include);
            foreach (var entity in entities)
            {
                UpdateNode(context, entity);
            }
        }

        public void Insert(DbTable table, params object[] values)
        {
            Parallel.ForEach(values, value =>
            {
                var map = new TableMapEntry(value.GetType(), table, OperationType.Insert);
                var ctx = new TableContext(_model, map);
                var opCtx = ctx.InsertUsingTemplate(ctx, value);

                ExecuteInsert(opCtx, table);

            });
        }
        
        public void Delete<T>(T entity, params string[] include) where T : class
        {
            if (typeof(T).IsCollection())
            {
                throw new ArgumentException("To delete all entities in a collection you need to explicitly provide the entity type as a generic argument to the delete method.");
            }
            var context = _model.CreateContext<T>(include);
            if (include != null && include.Any())
            {
                DeleteIncludingReferred(context, entity);
            }
            else
            {
                DeleteNode(context, entity);
            }
        }

        public void Delete<T>(IEnumerable<T> entities, params string[] include) where T : class
        {
            var context = _model.CreateContext<T>(include);
            foreach (var entity in entities)
            {
                if (include != null && include.Any())
                {
                    DeleteIncludingReferred(context, entity);
                }
                else
                {
                    DeleteNode(context, entity);
                }
            } 
        }

        public void Patch<T>(object dto) where T : class
        {
            var context = _model.CreateContext<T>();
            PatchNode(context, dto);
        }

        


        private void SaveNode(ITableContextNode node, object instance, Dictionary<string,object> unmappedValues = null)
        {
            var keys = node.Table.GetKeys();
            if (keys.Length > 1)
            {
                throw new NotSupportedException($"You need to use specific insert or update methods for entities with composite primary key (table: {node.Table}).");
            }
            var keyColumn = keys.SingleOrDefault();
            if (instance != null)
            {
                var keyValue = node.MapEntry.GetValueForColumn(instance, keyColumn);
                if (keyValue == null || keyValue.Equals(ReflectionHelper.GetDefaultValue(keyValue.GetType())) ||
                    (Options.EnableIdentityInsert && !_entities.ContainsKey(instance)))
                {
                    InsertNode(node, instance, unmappedValues);
                }
                else
                {
                    UpdateNode(node, instance, unmappedValues);
                }
            }
        }

        private void InsertNode(ITableContextNode node, object instance, Dictionary<string, object> unmappedValues = null)
        {
            if ((node.MapEntry.Operations & OperationType.Insert) == 0)
            {
                throw new InvalidOperationException($"Entity is not allowed to perform inserts on the table '{node.Table.TableName}'");
            }

            ProcessDependencies(node, instance);

            //Process this
            if ((Operations & OperationType.Insert) == 0) return;
            
            object pk;
            
            if (_entities.ContainsKey(instance))
            {
                pk = _entities[instance];
            }
            else
            {
                var opCtx = node.Context.Insert(node, instance, unmappedValues);
                pk = ExecuteInsert(opCtx, node.Table);
                _entities.Add(instance, pk);
            }
            if (pk != null && !(pk is DBNull))
            {
                var key = node.Table.GetSingularKey();
                var keyMember = node.MapEntry.GetMappedMember(key);
                keyMember.SetValue(instance, pk);

                //Process inverse dependant nodes
                ProcessInverseDependencies(node, instance, pk, OperationType.Insert);
            }
        }

        private object ExecuteInsert(OperationContext opCtx, DbTable table)
        {
            var keys = table.GetKeys();
            object pk;

            var insertWriter = _model.ResourceLocator.Get<IInsertStatementWriter>();
            var stm = insertWriter.GetStatement(opCtx, Options);

            if (keys.Length == 1 && keys[0].DefaultValue?.Expression == DbExpressionType.PrimaryKeySequence &&
                Options.EnableIdentityInsert &&
                (
                    (Options.Parameterize && stm.Args.ContainsKey("@key")) ||
                    (!Options.Parameterize && stm.Script.ToString().Contains(keys[0].ColumnName))
                ))
            {
                //TODO: Abstract behind interface
                stm.Script.WrapInside(
                    $"SET IDENTITY_INSERT {table} ON",
                    $"SET IDENTITY_INSERT {table} OFF",
                    false);

                CommandExecutor.ExecuteNonQuery(Command, stm);
                pk = opCtx.Args["@key"];
            }
            else
            {
                pk = CommandExecutor.ExecuteScalar(Command, stm);
            }

            return pk;
        }

        private void UpdateNode(ITableContextNode node, object instance, Dictionary<string, object> unmappedValues = null)
        {
            if ((node.MapEntry.Operations & OperationType.Update) == 0)
            {
                throw new InvalidOperationException($"Entity is not allowed to perform updates on the table '{node.Table.TableName}'");
            }

            ProcessDependencies(node, instance);

            //Process this
            
            if ((Operations & OperationType.Update) != 0)
            {
                var writer = _model.ResourceLocator.Get<IUpdateStatementWriter>();
                var opCtx = node.Context.Update(node, instance, unmappedValues);
                var stm = writer.GetStatement(opCtx, Options);
                CommandExecutor.ExecuteNonQuery(Command, stm);
            }
            if (node.Table.HasKey && !node.Table.HasCompositeKey)
            {
                var pk = node.MapEntry.GetValueForColumn(instance, node.Table.GetSingularKey());
                ProcessInverseDependencies(node, instance, pk, OperationType.Update);
            }
        }

        private void DeleteNode(ITableContextNode node, object instance)
        {
            if ((node.MapEntry.Operations & OperationType.Delete) == 0)
            {
                throw new InvalidOperationException($"Entity is not allowed to perform deletes on the table '{node.Table.TableName}'");
            }
            if (node.Table.HasKey && !node.Table.HasCompositeKey)
            {
                var pk = node.MapEntry.GetValueForColumn(instance, node.Table.GetSingularKey());

                ProcessInverseDependencies(node, instance, pk, OperationType.Delete);
            }
            if ((Operations & OperationType.Delete) != 0)
            {
                var writer = _model.ResourceLocator.Get<IDeleteStatementWriter>();
                var opCtx = node.Context.Delete(node, instance);
                var stm = writer.GetStatement(opCtx, Options);
                CommandExecutor.ExecuteNonQuery(Command, stm);
            }
        }

        private void PatchNode(ITableContextNode node, object instance)
        {
            if ((Operations & OperationType.Update) != 0)
            {
                var writer = _model.ResourceLocator.Get<IUpdateStatementWriter>();
                var opCtx = node.Context.Patch(node, instance);
                var stm = writer.GetStatement(opCtx, Options);
                CommandExecutor.ExecuteNonQuery(Command, stm);
            }
        }

        private void DeleteIncludingReferred(ITableContextNode node, object instance)
        {
            if (instance == null) return;

            DeleteNode(node, instance);

            var depNodes = node.Nodes.Where(r => !r.Value.IsInverted && !r.Value.Relationship.IsLookupRelationship);

            foreach (var item in depNodes)
            {
                var member = node.MapEntry.GetMemberByName(item.Key);
                var depInstance = member.GetValue(instance);
                if (depInstance == null) continue;
                DeleteIncludingReferred(item.Value, depInstance);
            }
        }

        private void ProcessDependencies(ITableContextNode node, object instance)
        {
            if (instance == null) return;

            var depNodes = node.Nodes.Where(r => !r.Value.IsInverted && !r.Value.Relationship.IsLookupRelationship);

            foreach (var item in depNodes)
            {
                var member = node.MapEntry.GetMemberByName(item.Key);
                var depInstance = member.GetValue(instance);
                if (depInstance == null) continue;
                SaveNode(item.Value, depInstance);
                var key = item.Value.MapEntry.GetValueForColumn(depInstance, item.Value.GetTargetKey);
                if (!node.MapEntry.SetValueForColumn(instance, item.Value.GetSourceKey, key))
                {
                    throw new InvalidOperationException("What to do?");
                }

            }
        }

        private void ProcessInverseDependencies(ITableContextNode node, object instance, object pk, OperationType context)
        {
            if (instance == null) return;

            var invNodes = node.Nodes.Where(r => r.Value.IsInverted && !r.Value.Relationship.IsLookupRelationship);

            Parallel.ForEach(invNodes, item =>
            {
                var member = node.MapEntry.GetMemberByName(item.Key);
                var collection = (ICollection)member.GetValue(instance);
                var keys = new List<object>();
                foreach (var invInstance in collection)
                {


                    if (context == OperationType.Insert || context == OperationType.Update)
                    {
                        Dictionary<string, object> unmappedValues = null;
                        if (!item.Value.MapEntry.SetValueForColumn(invInstance, item.Value.Relationship.ForeignKeyColumn, pk))
                        {
                            unmappedValues = new Dictionary<string, object> { { item.Value.Relationship.ForeignKeyColumn.AliasName, pk } };
                        }

                        SaveNode(item.Value, invInstance, unmappedValues);
                        var keyCol = item.Value.Table.GetSingularKey();
                        var invKey = item.Value.MapEntry.GetValueForColumn(invInstance, keyCol);
                        keys.Add(invKey);
                    }
                    else if (context == OperationType.Delete)
                    {
                        if (item.Value.Relationship.ForeignKeyColumn.IsNullable)
                        {
                            //Set foreign key to NULL with an update
                            SetForeignkeyToNull(item.Value, pk);
                        }
                        else
                        {
                            DeleteNode(item.Value, invInstance);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid operation type: {context}!");
                    }

                }
                if (context == OperationType.Update)
                {
                    var existingKeys = SelectKeysFromChildTable(item.Value, pk);

                    var toDelete = existingKeys.Except(keys).ToArray();
                    if (toDelete.Any())
                    {
                        foreach (var key in toDelete)
                        {
                            var instanceToRemove = GetInstanceByKey(item.Value, key);
                            DeleteNode(item.Value, instanceToRemove);
                        }
                    }
                }
            });
           
        }

        //TODO: This must be abstracted and moved to Database folder
        private void SetForeignkeyToNull(TableContextNode node, object pk)
        {
            var fkCol = node.Relationship.ForeignKeyColumn;
            var pkCol = node.Table.GetSingularKey();
            var parameter = new DbParameter("@key", pkCol.DataType, null, false);
            //TODO: Abstract behind interface
            var sql = $"UPDATE {node.Table} SET {fkCol.ColumnName}=NULL WHERE {pk} = {parameter.Name}";
            var stm = new SqlStatement();
            stm.Script.Add(sql);
            stm.Parameters.Add(parameter);
            stm.Args.Add(parameter.Name, pk);
            CommandExecutor.ExecuteNonQuery(Command, stm);
        }
        private object[] SelectKeysFromChildTable(TableContextNode node, object pk)
        {
            var fkCol = node.Relationship.ForeignKeyColumn;
            var pkCol = node.Table.GetSingularKey();

            var parameter = new DbParameter("@key", pkCol.DataType, null, false);
            //TODO: Abstract behind interface
            var sql = $"SELECT {pkCol.ColumnName} FROM {node.Table} WHERE {fkCol.ColumnName} = @key";
            var stm = new SqlStatement();
            stm.Script.Add(sql);

            stm.Parameters.Add(parameter);
            stm.Args.Add(parameter.Name, pk);

            var res = CommandExecutor.ExecuteQuery(Command, stm);

            return res.RecordSets.Single().Vector(0);
        }

        private object GetInstanceByKey(ITableContextNode node, object key)
        {
            var writer = _model.ResourceLocator.Get<ISelectStatementWriter>();
            var filter = FilterGraph.CreateByPrimaryKeyFilter(node, key);

            var instance = DbReader.ExecuteContextQuery(node, filter, Command, writer).Single();

            return instance;
        }

        

        
    }
}
