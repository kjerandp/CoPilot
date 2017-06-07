using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Exceptions;

namespace CoPilot.ORM.Model
{
    public class DbTable
    {
        private readonly HashSet<DbColumn> _columns = new HashSet<DbColumn>();
        private readonly HashSet<DbRelationship> _inverseRelationships = new HashSet<DbRelationship>();
        
        internal static Tuple<string, string> SanitizeTableName(string tableName)
        {
            string schema = null;
            
            tableName = tableName.Replace("[", "").Replace("]", "");
            var s = tableName.Split('.');

            if (s.Length == 1)
            {
                tableName = s[0];
            }
            else if (s.Length == 2)
            {
                schema = s[0];
                tableName = s[1];
            }
            else
            {
                throw new CoPilotConfigurationException($"'{tableName}' is an invalid table name.");
            }
            return new Tuple<string, string>(schema, tableName);
        } 

        public DbTable(string tableName, string schemaName)
        {
            TableName = tableName.Replace("[", "").Replace("]", ""); 
            Schema = schemaName.Replace("[", "").Replace("]", ""); 
        }

        public string TableName { get; }
        public string Schema { get; }
        //public Type MappedToType { get; }
        public DbColumn[] Columns => _columns.ToArray();
        public DbRelationship[] InverseRelationships => _inverseRelationships.ToArray();
        public bool HasKey => _columns.Any(r => r.IsPrimaryKey);
        public bool HasCompositeKey => _columns.Count(r => r.IsPrimaryKey) > 1;
        public DbColumn AddColumn(string columnName, DbDataType dataType = DbDataType.Unknown, string alias = null)
        {
            if (alias != null && _columns.Any(r => r.AliasName.Equals(alias, StringComparison.OrdinalIgnoreCase)))
            {
                throw new CoPilotConfigurationException("Column alias already used for a column in this table!");
            }
            var col = new DbColumn(this, columnName, alias) {DataType = dataType};

            if (!_columns.Add(col))
            {
                throw new CoPilotConfigurationException("Column already exist!");
            }
            return col;
        }
        public DbColumn GetColumn(ClassMemberInfo member)
        {
            return _columns.SingleOrDefault(r => r.IsMappedTo(member));
        }
        public DbColumn GetColumnByName(string columnName, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return _columns.SingleOrDefault(r => r.ColumnName.Equals(columnName, stringComparison));
        }
        public DbColumn GetColumnByAlias(string aliasName, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return _columns.SingleOrDefault(r => r.AliasName.Equals(aliasName, stringComparison));
        }
        public DbColumn GetColumnByMemberName(string memberName)
        {
            return _columns.SingleOrDefault(r => r.IsMappedToMemberName(memberName));
        }
        public DbColumn GetColumnByMember(ClassMemberInfo member)
        {
            return _columns.SingleOrDefault(r => r.IsMappedTo(member));
        }
        public DbColumn[] GetMappedColumns(Type entity)
        {
            return _columns.Where(r => r.MappedMembers.Any(x => entity.GetTypeInfo().IsAssignableFrom(x.DeclaringClassType))).ToArray();
        }
        public DbRelationship[] Relationships => _columns.Where(r => r.IsForeignKey).Select(r => r.ForeignkeyRelationship).ToArray();
        public DbRelationship[] AllRelationships => Relationships.Union(InverseRelationships).ToArray();
        public bool IsRelated => _columns.Any(r => r.IsForeignKey) || _inverseRelationships.Any();
        
        internal void AddInverseRelationship(DbRelationship relationship)
        {
            _inverseRelationships.Add(relationship);
        }       
           
        public DbColumn[] GetKeys()
        {
            return _columns.Where(r => r.IsPrimaryKey).ToArray(); //TODO: Should have ordered keys
        }

        public DbColumn GetSingularKey()
        {
            var keys = GetKeys();
            if(keys.Length > 1) throw new CoPilotUnsupportedException("This operation is not supported for entities with composite primary key!");
            return keys.SingleOrDefault();
        }

        public override string ToString()
        {
            return $"[{Schema}].[{TableName}]";
        }
        
        public Dictionary<DbColumn, ClassMemberInfo> GetColumnsByAlias(ClassMemberInfo[] props)
        {
            return _columns.Join(props, c => c.AliasName.ToLower(), p => p.Name.ToLower(), (c, p) => new {c, p})
                .ToDictionary(k => k.c, v => v.p);
        }
    }
}
