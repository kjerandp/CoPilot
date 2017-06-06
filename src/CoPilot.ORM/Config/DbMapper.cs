using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CoPilot.ORM.Common;
using CoPilot.ORM.Config.Builders;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Config.Naming;
using CoPilot.ORM.Database;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config
{
    /// <summary>
    /// Offers a simple fluent API to map your POCO models to database tables and provide other configurations.
    /// Use this class to create the DbModel and to create an instance of CoPilot's IDb interface.
    /// </summary>
    public class DbMapper
    {
        private readonly DbModel _model;


        public DbMapper()
        {
            _model = new DbModel();
            DefaultAllowedOperations = OperationType.Select | OperationType.Update | OperationType.Insert;
        }
        /// <summary>
        /// True if model is created and finalized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Set default operations allowed on mapped tables. 
        /// <remarks>
        /// Select, Update and Insert are set by default, which means you will have to explicitly add the Delete operations to the entities you want to be able to delete records from (when using the IDb interface or the DbWriter class). 
        /// </remarks>
        /// </summary>
        public OperationType DefaultAllowedOperations { get; set; }

        /// <summary>
        /// Map a database table
        /// </summary>
        /// <param name="tableName">Name of database tabel</param>
        /// <returns>Table builder to chain table specific configurations</returns>
        public TableBuilder Map(string tableName)
        {

            var tbl = _model.GetTable(tableName) ?? _model.AddTable(tableName);
            return new TableBuilder(_model, tbl);
        }

        /// <summary>
        /// Map a POCO class to a database table
        /// </summary>
        /// <param name="tableName">Name of database tabel</param>
        /// <param name="operations">Operations to be allowed on this entity</param>
        /// <param name="keyMemberName">Name of property in POCO class that is mapped to the PK column</param>
        /// <param name="keyColumnName">The primary key column name 
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <returns>Table builder to chain table specific configurations</returns>
        public TableBuilder<T> Map<T>(string tableName, OperationType operations, string keyMemberName = "Id",
            string keyColumnName = null) where T : class
        {
            var builder = Map<T>(tableName, keyMemberName, keyColumnName);
            builder.Operations(operations);
            return builder;
        }

        /// <summary>
        /// Map a POCO class to a database table with the default operations allowed
        /// </summary>
        /// <param name="tableName">Name of database tabel</param>
        /// <param name="keyMemberName">Name of property in POCO class that is mapped to the PK column</param>
        /// <param name="keyColumnName">The primary key column name 
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <returns>Table builder to chain table specific configurations</returns>
        public TableBuilder<T> Map<T>(string tableName, string keyMemberName = "Id", string keyColumnName = null) where T : class
        {
            var sanitized = DbTable.SanitizeTableName(tableName);
            var tbl = _model.GetTable(tableName) ?? _model.AddTable(sanitized.Item2, sanitized.Item1);

            var map = _model.AddTableMap(typeof(T), tbl, DefaultAllowedOperations);

            var builder = new TableBuilder<T>(_model, map);
            if (!string.IsNullOrEmpty(keyMemberName))
            {
                var keyMember = typeof(T).GetTypeInfo().GetMember(keyMemberName).FirstOrDefault();
                
                if (keyMember != null)
                {
                    var cb = builder.Column(ClassMemberInfo.Create(keyMember), keyColumnName);

                    cb.IsKey();
                    cb.DefaultValue(DefaultValue.PrimaryKey);
                }
            }
            return builder;
        }

        /// <summary>
        /// Map a POCO class to a database table with the default operations allowed
        /// </summary>
        /// <param name="tableName">Name of database tabel</param>
        /// <param name="key">Expression to select the property in POCO class that is mapped to the PK column</param>
        /// <param name="keyColumnName">The primary key column name 
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <returns>Table builder to chain table specific configurations</returns>
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

        /// <summary>
        /// Create the model based on input config. <remarks>CoPilot will make assumptions and auto-map simple valued
        /// properties that has not explicitly been configured to be ignored.</remarks>
        /// </summary>
        /// <returns>Instance of DbModel</returns>
        public DbModel CreateModel()
        {
            if(IsInitialized) throw new CoPilotRuntimeException("Model is already created!");

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

        /// <summary>
        /// Creates and returns an implementation of the CoPilot IDb interface 
        /// </summary>
        /// <param name="connectionString">Connection string to database</param>
        /// <param name="dbProvider">Database provider</param>
        /// <returns>Instance of the CoPilot interface (IDb)</returns>
        public IDb CreateDb(string connectionString, IDbProvider dbProvider)
        {
            if (!IsInitialized) CreateModel();

            return _model.CreateDb(connectionString, dbProvider);
        }

        /// <summary>
        /// Creates and returns an implementation of the CoPilot IDb interface without any mapping
        /// </summary>
        /// <param name="connectionString">Connection string to database</param>
        /// <param name="dbProvider">Database provider</param>
        /// <returns>Instance of the CoPilot interface (IDb)</returns>
        public static IDb Create(string connectionString, IDbProvider dbProvider)
        {
            var m = new DbMapper();
            return m.CreateDb(connectionString, dbProvider);
        }

        /// <summary>
        /// Map a stored procedure. Use this if you plan on calling any stored procedures that has default 
        /// parameters or if you want to pass in POCO object instances as arguments instead of anonymous objects.
        /// </summary>
        /// <param name="procName">Stored procedure name <remarks>Wrap inside brackets [] if name contains spaces</remarks></param>
        /// <param name="parameters">Parameters if any<see cref="DbParameter"/></param>
        /// <returns></returns>
        public StoredProcedureBuilder MapProc(string procName, params DbParameter[] parameters)
        {
            var proc = _model.GetStoredProcedure(procName) ?? _model.AddStoredProcedure(procName);
            var builder = new StoredProcedureBuilder(_model, proc);
            builder.Parameters(parameters);
            return builder;
        }

        /// <summary>
        /// Set default schema name to use. Default is dbo
        /// </summary>
        /// <param name="schemaName">Name of database schema</param>
        public void SetDefaultSchema(string schemaName)
        {
            _model.DefaultSchemaName = string.IsNullOrEmpty(schemaName) ? "dbo" : schemaName;
        }

        /// <summary>
        /// Used to specify a specific naming convention when mapping properties to columns. Default is upper case snake-case prefixed by table name
        /// </summary>
        /// <param name="columnNamingConvention"><see cref="DbColumnNamingConvention"/> <seealso cref="ILetterCaseConverter"/></param>
        public void SetColumnNamingConvention(DbColumnNamingConvention columnNamingConvention)
        {
            _model.ColumnNamingConvention = columnNamingConvention;
        }
    }
}
