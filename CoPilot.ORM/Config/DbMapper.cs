using System;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Common.Config;
using CoPilot.ORM.Config.Builders;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Config.Naming;
using CoPilot.ORM.Database;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config
{
    public class DbMapper
    {
        private readonly DbModel _model;


        public DbMapper()
        {
            var resourceLocator = new ResourceLocator();
            Defaults.RegisterDefaults(resourceLocator);
            _model = new DbModel(resourceLocator);
            DefaultAllowedOperations = OperationType.Select | OperationType.Update | OperationType.Insert;
        }


        public bool IsInitialized { get; private set; }
        public OperationType DefaultAllowedOperations { get; set; }

        public TableBuilder Map(string tableName)
        {

            var tbl = _model.GetTable(tableName) ?? _model.AddTable(tableName);
            return new TableBuilder(_model, tbl);
        }

        public TableBuilder<T> Map<T>(string tableName, OperationType operations, string keyMemberName = "Id",
            string keyColumnName = null) where T : class
        {
            var builder = Map<T>(tableName, keyMemberName, keyColumnName);
            builder.Operations(operations);
            return builder;
        }
        public TableBuilder<T> Map<T>(string tableName, string keyMemberName = "Id", string keyColumnName = null) where T : class
        {
            var sanitized = DbTable.SanitizeTableName(tableName);
            var tbl = _model.GetTable(tableName) ?? _model.AddTable(sanitized.Item2, sanitized.Item1);

            var map = _model.AddTableMap(typeof(T), tbl, DefaultAllowedOperations);

            var builder = new TableBuilder<T>(_model, map);
            if (!string.IsNullOrEmpty(keyMemberName))
            {
                var keyMember = typeof(T).GetMember(keyMemberName).FirstOrDefault();
                //if (keyMember == null)
                //    throw new ArgumentException(
                //        $"Looking for field named '{keyMemberName}', but it was not found!");
                if (keyMember != null)
                {
                    var cb = builder.Column(ClassMemberInfo.Create(keyMember), keyColumnName);

                    cb.IsKey();
                    cb.DefaultValue(DefaultValue.PrimaryKey);
                }
            }
            return builder;
        }
        public TableBuilder<T> Map<T>(string tableName, Expression<Func<T, object>> key, string keyColumnName = null) where T : class
        {
            var tbl = _model.GetTable(tableName) ?? _model.AddTable(tableName);

            var map = _model.AddTableMap(typeof(T), tbl, DefaultAllowedOperations);

            var builder = new TableBuilder<T>(_model, map);

            if (key != null)
            {
                builder.AddKey(key, keyColumnName);
            }

            return builder;
        }

        internal static string TransformColumnName(DbTable table, string input)
        {
            return input.Replace("~", table.TableName + "_").ToUpper();
        }

        public DbModel CreateModel()
        {
            if(IsInitialized) throw new InvalidOperationException("Model is already created!");

            foreach (var map in _model.GetAllTableMaps())
            {
                var tbl = map.Table;
                var members = map.EntityType.GetClassMembers().Except(_model.Ignored);
                foreach (var member in members.Where(r => r.MemberType.IsSimpleValueType()))
                {
                    var col = tbl.GetColumn(member);
                    if (col == null)
                    {
                        var columnName = _model.GenerateColumnName(tbl, member);
                        col = tbl.GetColumnByName(columnName);
                        if (col == null)
                        {
                            col = tbl.AddColumn(columnName, member.DataType);
                            col.IsNullable = member.MemberType.IsNullable();
                            if (col.DataType == DbDataType.String)
                            {
                                col.MaxSize = DbConversionHelper.DefaultDbStringSize;
                            }
                        }
                        col.MapToMember(member);
                        
                    }  
                }
            }
            IsInitialized = true;
            return _model;
        }

        public IDb CreateDb(string connectionString)
        {
            if (!IsInitialized) CreateModel();

            return _model.CreateDb(connectionString);
        }

        public static IDb Create(string connectionString)
        {
            var m = new DbMapper();
            return m.CreateDb(connectionString);
        }

        public StoredProcedureBuilder MapProc(string procName, params DbParameter[] parameters)
        {
            var proc = _model.GetStoredProcedure(procName) ?? _model.AddStoredProcedure(procName);
            var builder = new StoredProcedureBuilder(_model, proc);
            builder.Parameters(parameters);
            return builder;
        }

        public void SetDefaultSchema(string schemaName)
        {
            _model.DefaultSchemaName = string.IsNullOrEmpty(schemaName) ? "dbo" : schemaName;
        }


        public void SetColumnNamingConvention(DbColumnNamingConvention columnNamingConvention)
        {
            _model.ColumnNamingConvention = columnNamingConvention;
        }
    }
}
