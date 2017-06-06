using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CoPilot.ORM.Common;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database.Commands
{
    /// <summary>
    /// Use this class to perform database write operations as a unit of work (using database transaction) 
    /// </summary>
    public class DbWriter: IDisposable
    {
        /// <summary>
        /// Set which operations CoPilot is allowed to execute. Default is all.
        /// </summary>
        public OperationType Operations { get; set; }
        private readonly Dictionary<object,object> _entities = new Dictionary<object, object>();
        private readonly DbModel _model;

        private bool _isCommited;

        private readonly IDbProvider _provider;
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;
        private readonly IDbCommand _command;

        /// <summary>
        /// <see cref="ScriptOptions"/>
        /// </summary>
        public ScriptOptions Options;

        


        /// <summary>
        /// Create an instance of the DbWriter with default behaviours
        /// </summary>
        /// <param name="db">CoPilot interface implementation</param>
        public DbWriter(IDb db) : this(db, ScriptOptions.Default()){}

        /// <summary>
        /// Internal use only
        /// </summary>
        /// <param name="db">CoPilot interface implementation</param>
        /// <param name="options">Options allows to control parameterization, identity insert etc <see cref="ScriptOptions"/></param>
        /// <param name="isolation"></param>
        /// <param name="timeout"></param>
        internal DbWriter(IDb db, ScriptOptions options, IsolationLevel isolation = IsolationLevel.ReadCommitted, int timeout = 30) 
        {
            Options = options ?? ScriptOptions.Default();
            Operations = (OperationType.Insert | OperationType.Update | OperationType.Delete);

            _model = db.Model;
            _provider = db.DbProvider;
            _connection = db.Connection;
            _connection.Open();
            //_transactionId = "T" + DateTime.Now.ToFileTime();
            _transaction = _connection.BeginTransaction(isolation);
            _command = _provider.CreateCommand(_connection, timeout);
            _command.CommandType = CommandType.Text;
            _command.Transaction = _transaction;

        }

        /// <summary>
        /// Same as Command-method in the IDb interface <seealso cref="IDb.Command"/>
        /// </summary>
        /// <param name="commandText">Parameterized sql statement</param>
        /// <param name="args">Anonymous object for passing arguments to named parameters</param>
        /// <returns>Rows affected</returns>
        public int Command(string commandText, object args = null)
        {
            var request = DbRequest.CreateRequest(_model, commandText, args);
            request.Command = _command;
            return _provider.ExecuteNonQuery(request);
        }

        public void PrepareCommand(string commandText, object template)
        {
            var request = DbRequest.CreateRequest(_model, commandText, template);
            request.Command = _command;
            _provider.PrepareNonQuery(request);
        }

        public int Command(object args)
        {
            if(_command.Parameters == null || string.IsNullOrEmpty(_command.CommandText))
                throw new CoPilotUnsupportedException("Cannot re-run a command without parameters and/or statement set!");

            return _provider.ReRunCommand(_command, args);
        }

        public int BulkCommand(string commandText, IList<object> args)
        {
            if (args == null || !args.Any())
                throw new CoPilotUnsupportedException("Can't execute bulk command without any arguments!");

            var request = DbRequest.CreateRequest(_model, commandText, args[0]);
            request.Command = _command;
            var result = _provider.ExecuteNonQuery(request);
            for (var i = 1; i < args.Count; i++)
            {
                var r = _provider.ReRunCommand(_command, args[i]);
                if (result >= 0 && r > -1) result += r;
                if (r < 0) result = r;
            }
            return result; 
        }

        /// <summary>
        /// Same as Scalar-method in the IDb interface <seealso cref="IDb.Scalar"/>
        /// </summary>
        /// <param name="commandText">Parameterized sql statement</param>
        /// <param name="args">Anonymous object for passing arguments to named parameters</param>
        /// <returns>Scalar value</returns>
        public object Scalar(string commandText, object args = null)
        {
            var request = DbRequest.CreateRequest(_model, commandText, args);
            request.Command = _command;
            return _provider.ExecuteScalar(request);
        }

        /// <summary>
        /// Same as Scalar-method in the IDb interface <seealso cref="IDb.Scalar"/>
        /// </summary>
        /// <param name="commandText">Parameterized sql statement</param>
        /// <param name="args">Anonymous object for passing arguments to named parameters</param>
        /// <returns>Scalar value converted to type of T</returns>
        public T Scalar<T>(string commandText, object args = null)
        {
            object convertedValue;
            ReflectionHelper.ConvertValueToType(typeof(T), Scalar(commandText, args), out convertedValue);

            return (T)convertedValue;
        }

        /// <summary>
        /// Same as Save method in IDb interface <see>
        ///         <cref>CoPilotExtensions.Save{T}(T,string[])</cref>
        ///     </see>
        /// </summary>
        /// <typeparam name="T">POCO class for context</typeparam>
        /// <param name="entity">Instance of POCO</param>
        /// <param name="include">Included entities</param>
        public void Save<T>(T entity, params string[] include) where T : class
        {
            if (typeof(T).IsCollection())
            {
                throw new CoPilotUnsupportedException("To save all entities in a collection you need to explicitly provide the entity type as a generic argument to the save method.");
            }
            var context = _model.CreateContext<T>(include);
            SaveNode(context, entity);
        }

        /// <summary>
        /// Same as batch Save method in IDb interface <see>
        ///         <cref>IDb.Save{T}(IEnumerable{T},string[])</cref>
        ///     </see>
        /// </summary>
        /// <typeparam name="T">POCO class for context</typeparam>
        /// <param name="entities">Collection of instances</param>
        /// <param name="include">Included entities</param>
        public void Save<T>(IEnumerable<T> entities, params string[] include) where T : class
        {
            var context = _model.CreateContext<T>(include);
            foreach (var entity in entities)
            {
                SaveNode(context, entity);
            }
            
        }

        /// <summary>
        /// Method for explicitly issuing an insert statement. Required if the entity doesn't have a singular primary key defined.
        /// </summary>
        /// <typeparam name="T">POCO class for context</typeparam>
        /// <param name="entity">Instance of POCO to insert</param>
        /// <param name="include">Included entities</param>
        public void Insert<T>(T entity, params string[] include) where T : class
        {
            if (typeof(T).IsCollection())
            {
                throw new CoPilotUnsupportedException("To insert all entities in a collection you need to explicitly provide the entity type as a generic argument to the insert method.");
            }
            var context = _model.CreateContext<T>(include);
            InsertNode(context, entity);
        }

        /// <summary>
        /// Batch version of Insert.
        /// </summary>
        /// <typeparam name="T">POCO class for context</typeparam>
        /// <param name="entities">Collection of instances</param>
        /// <param name="include">Included entities</param>
        public void Insert<T>(IEnumerable<T> entities, params string[] include) where T : class
        {
            var context = _model.CreateContext<T>(include);
            foreach (var entity in entities)
            {
                InsertNode(context, entity);
            }
        }

        /// <summary>
        /// Method for explicitly issuing an update statement. Required if the entity doesn't have a singular primary key defined.
        /// </summary>
        /// <typeparam name="T">POCO class for context</typeparam>
        /// <param name="entity">Instance of POCO to update</param>
        /// <param name="include">Included entities</param>
        public void Update<T>(T entity, params string[] include) where T : class
        {
            if (typeof(T).IsCollection())
            {
                throw new CoPilotUnsupportedException("To update all entities in a collection you need to explicitly provide the entity type as a generic argument to the update method.");
            }
            var context = _model.CreateContext<T>(include);
            UpdateNode(context, entity);
        }

        /// <summary>
        /// Batch version of Update.
        /// </summary>
        /// <typeparam name="T">POCO class for context</typeparam>
        /// <param name="entities">Collection of instances</param>
        /// <param name="include">Included entities</param>
        public void Update<T>(IEnumerable<T> entities, params string[] include) where T : class
        {
            var context = _model.CreateContext<T>(include);
            foreach (var entity in entities)
            {
                UpdateNode(context, entity);
            }
        }

        /// <summary>
        /// Used to insert values into unmapped database tables 
        /// </summary>
        /// <param name="table">Description of the database table <see cref="DbTable"/> <seealso cref="DbColumn"/></param>
        /// <param name="values">Single or multiple objects containing values to insert (normally provided with anonymous objects)</param>
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

        /// <summary>
        /// Same as Delete method in IDb interface <see cref="IDb.Delete{T}(T,string[])"/>
        /// </summary>
        /// <typeparam name="T">POCO class for context</typeparam>
        /// <param name="entity">Instance of POCO</param>
        /// <param name="include">Included entities</param>
        public void Delete<T>(T entity, params string[] include) where T : class
        {
            if (typeof(T).IsCollection())
            {
                throw new CoPilotUnsupportedException("To delete all entities in a collection you need to explicitly provide the entity type as a generic argument to the delete method.");
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

        /// <summary>
        /// Batch version of Delete.
        /// </summary>
        /// <typeparam name="T">POCO class for context</typeparam>
        /// <param name="entities">Collection of instances</param>
        /// <param name="include">Included entities</param>
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

        /// <summary>
        /// Same as Patch method in IDb interface <see cref="IDb.Patch{T}(object)"/>
        /// </summary>
        /// <typeparam name="T">POCO class for context</typeparam>
        /// <param name="dto">Dto object to patch from</param>
        public void Patch<T>(object dto) where T : class
        {
            var context = _model.CreateContext<T>();
            PatchNode(context, dto);
        }

        public DbReader GetReader()
        {
            return new DbReader(_provider, _command, _model);
        }

        private void SaveNode(ITableContextNode node, object instance, Dictionary<string,object> unmappedValues = null)
        {
            var keys = node.Table.GetKeys();
            if (keys.Length > 1)
            {
                throw new CoPilotUnsupportedException($"You need to use specific insert or update methods for entities with composite primary key (table: {node.Table}).");
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
                throw new CoPilotRuntimeException($"Entity is not allowed to perform inserts on the table '{node.Table.TableName}'");
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

            
            var stm = _provider.InsertStatementWriter.GetStatement(opCtx, Options);
            stm.Command = _command;

            if (keys.Length == 1 && keys[0].DefaultValue?.Expression == DbExpressionType.PrimaryKeySequence &&
                Options.EnableIdentityInsert &&
                (
                    (Options.Parameterize && stm.Args != null && stm.Args.ContainsKey("@key")) ||
                    (!Options.Parameterize && stm.Script.ToString().Contains(keys[0].ColumnName))
                ))
            {
                
                stm.Script = _provider.CommonScriptingTasks.WrapInsideIdentityInsertScript(table.ToString(), stm.Script);
                
                _provider.ExecuteNonQuery(stm);
                pk = opCtx.Args["@key"];
            }
            else
            {
                pk = _provider.ExecuteScalar(stm);
            }

            return pk;
        }

        private void UpdateNode(ITableContextNode node, object instance, Dictionary<string, object> unmappedValues = null)
        {
            if ((node.MapEntry.Operations & OperationType.Update) == 0)
            {
                throw new CoPilotRuntimeException($"Entity is not allowed to perform updates on the table '{node.Table.TableName}'");
            }

            ProcessDependencies(node, instance);

            //Process this
            
            if ((Operations & OperationType.Update) != 0)
            {
                
                var opCtx = node.Context.Update(node, instance, unmappedValues);
                var stm = _provider.UpdateStatementWriter.GetStatement(opCtx, Options);
                stm.Command = _command;
                _provider.ExecuteNonQuery(stm);
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
                throw new CoPilotRuntimeException($"Entity is not allowed to perform deletes on the table '{node.Table.TableName}'");
            }
            if (node.Table.HasKey && !node.Table.HasCompositeKey)
            {
                var pk = node.MapEntry.GetValueForColumn(instance, node.Table.GetSingularKey());

                ProcessInverseDependencies(node, instance, pk, OperationType.Delete);
            }
            if ((Operations & OperationType.Delete) != 0)
            {
                
                var opCtx = node.Context.Delete(node, instance);
                var stm = _provider.DeleteStatementWriter.GetStatement(opCtx, Options);
                stm.Command = _command;
                _provider.ExecuteNonQuery(stm);
            }
        }

        private void PatchNode(ITableContextNode node, object instance)
        {
            if ((Operations & OperationType.Update) != 0)
            {

                var opCtx = node.Context.Patch(node, instance);
                var stm = _provider.UpdateStatementWriter.GetStatement(opCtx, Options);
                stm.Command = _command;
                _provider.ExecuteNonQuery(stm);
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
                    throw new CoPilotRuntimeException("Unable to set value for column while processing dependencies.");
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
                        throw new CoPilotUnsupportedException($"Invalid operation type: {context}!");
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
                            var instanceToRemove = GetReader().FindByKey(item.Value, key);
                            DeleteNode(item.Value, instanceToRemove);
                        }
                    }
                }
            });
           
        }

        private void SetForeignkeyToNull(TableContextNode node, object pk)
        {
            var fkCol = node.Relationship.ForeignKeyColumn;
            var pkCol = node.Table.GetSingularKey();
            var parameter = new DbParameter("@key", pkCol.DataType, null, false);
            var stm = new SqlStatement(_provider.CommonScriptingTasks.SetForeignKeyValueToNullScript(node.Table.ToString(),
                    fkCol.ColumnName, pkCol.ColumnName)) {Command = _command};
            stm.Parameters.Add(parameter);
            stm.AddArgument(parameter.Name, pk);

            _provider.ExecuteNonQuery(stm);
        }

        private object[] SelectKeysFromChildTable(TableContextNode node, object pk)
        {
            var fkCol = node.Relationship.ForeignKeyColumn;
            var pkCol = node.Table.GetSingularKey();

            var parameter = new DbParameter("@key", pkCol.DataType, null, false);
            var stm = new SqlStatement(_provider.CommonScriptingTasks.GetSelectKeysFromChildTableScript(
                    node.Table.ToString(), pkCol.ColumnName, fkCol.ColumnName)) {Command = _command};
            stm.Parameters.Add(parameter);
            stm.Args.Add(parameter.Name, pk);

            var res = _provider.ExecuteQuery(stm);

            return res.RecordSets.Single().Vector(0);
        }
        public void Commit()
        {
            if (_isCommited) return;

            _transaction.Commit();
            _isCommited = true;
        }

        public void Rollback()
        {
            _transaction.Rollback();
        }
        public void Dispose()
        {
            _connection.Close();
            _command.Dispose();
            _transaction.Dispose();
            _connection.Dispose();
        }
    }
}
