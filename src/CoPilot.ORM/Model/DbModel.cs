using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Common.Config;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Config.Naming;
using CoPilot.ORM.Context;
using CoPilot.ORM.Database;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Mapping;

namespace CoPilot.ORM.Model
{
    public class DbModel
    {
        private readonly Dictionary<Type, TableMapEntry> _tableMappings;

        internal HashSet<ClassMemberInfo> Ignored = new HashSet<ClassMemberInfo>();
        internal readonly HashSet<DbTable> Tables = new HashSet<DbTable>();
        internal readonly HashSet<DbStoredProcedure> StoredProcedures = new HashSet<DbStoredProcedure>();

        internal DbColumnNamingConvention ColumnNamingConvention { get; set; }
        internal string DefaultSchemaName { get; set; }

        public ResourceLocator ResourceLocator { get; }
        
        public DbModel(ResourceLocator resourceLocator)
        {
            ResourceLocator = resourceLocator;
            DefaultSchemaName = "dbo";
            _tableMappings = new Dictionary<Type, TableMapEntry>();

        }

        internal DbTable AddTable(string tableName, string schema = null)
        {
            if (schema == null)
            {
                var sanitized = DbTable.SanitizeTableName(tableName);
                schema = sanitized.Item1 ?? DefaultSchemaName;
                tableName = sanitized.Item2;
            } 
            var t = new DbTable(tableName, schema);
            if (Tables.Add(t)) return t;

            throw new CoPilotRuntimeException("Unable to add table to collection!");
        }

        public DbTable GetTable(string tableName, string schemaName = null)
        {
            return Tables.SingleOrDefault(r => r.Schema.Equals(schemaName ?? DefaultSchemaName, StringComparison.OrdinalIgnoreCase) && r.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        }
        public DbStoredProcedure GetStoredProcedure(string procName)
        {
            return StoredProcedures.SingleOrDefault(r => r.ProcedureName.Equals(procName, StringComparison.OrdinalIgnoreCase));
        }

        public DbStoredProcedure AddStoredProcedure(string procName)
        {
            var p = new DbStoredProcedure(procName);
            if (StoredProcedures.Add(p)) return p;

            throw new CoPilotRuntimeException("Unable to add stored procedure to collection!");
        }

        public TableMapEntry GetTableMap(Type entityType)
        {
            if (_tableMappings.ContainsKey(entityType))
            {
                return _tableMappings[entityType];
            }
            return null;
        }

        public TableMapEntry GetTableMap<T>() where T : class
        {
            return GetTableMap(typeof(T));
        }

        public bool IsMapped(Type type)
        {
            return _tableMappings.ContainsKey(type);
        }

        public bool IsMapped<T>() where T : class 
        {
            return IsMapped(typeof(T));
        }

        public TableMapEntry AddTableMap(Type entityType, DbTable table, OperationType operations = OperationType.Select)
        {
            if (_tableMappings.ContainsKey(entityType))
            {
                throw new CoPilotConfigurationException($"Type '{entityType.Name}' is already mapped to a table!");
            }
            var map =  new TableMapEntry(entityType, table, operations);
            _tableMappings.Add(entityType, map);
            return map;
        }
        
        public TableContext<T> CreateContext<T>(params string[] include) where T : class
        {
            //TODO: Cache contexts
            return new TableContext<T>(this, include);
        }
        public TableContext<T> CreateContext<T>(Expression<Func<T, object>> selector, params string[] include) where T : class
        {
            var context = new TableContext<T>(this);
            context.ApplySelector(selector);
            return context;
        }

        public TableContext CreateContext(Type type, params string[] include) 
        {
            var context = new TableContext(this, type, include);
            return context;
        }

        public DbRelationship[] GetRelationshipsFromPath(Type entityType, string path)
        {
            var relationships = new HashSet<DbRelationship>();
            var baseMap = GetTableMap(entityType);
            if (baseMap == null || !baseMap.Table.IsRelated) return null;

            var currentMap = baseMap;
            var splitPaths = path.Split('.');
            foreach (var part in splitPaths)
            {
                var member = currentMap.GetMemberByName(part);
                var rel = currentMap.GetRelationshipByMember(member);
                if (rel == null) throw new CoPilotConfigurationException($"There are no relationships that corresponds to the path '{path}' for type '{entityType.Name}'.");
                relationships.Add(rel);
                currentMap = GetTableMap(member.MemberType);
            }

            return relationships.ToArray();
        }

        public Dictionary<string, DbRelationship[]> GetRelationshipsFromPaths(Type entityType, params string[] paths)
        {
            return paths.ToDictionary(path => path, path => GetRelationshipsFromPath(entityType, path));
        }

        internal IEnumerable<TableMapEntry> GetAllTableMaps()
        {
            return _tableMappings.Values;
        }

        internal string GenerateColumnName(DbTable table, ClassMemberInfo member)
        {
            var namer = ColumnNamingConvention ?? DbColumnNamingConvention.Default;

            return namer.Name(member.Name, table.TableName.Replace(" ", "_"));
        }

        public IDb CreateDb(string connectionString)
        {
            return new Db(this, connectionString);
        }
    }
}
