using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Config.DataTypes;

namespace CoPilot.ORM.Model
{
    public class DbColumn
    {
        private int? _maxSize;
        private bool _isPrimaryKey;
        private readonly HashSet<ClassMemberInfo> _mappedMembers = new HashSet<ClassMemberInfo>();

        internal DbColumn(DbTable table, string columnName, string aliasName = null)
        {
            ColumnName = columnName;
            Table = table;
            DataType = DbDataType.Unknown;
            AliasName = aliasName ?? columnName;
        }

        public string ColumnName { get; }
        public string AliasName { get; internal set; }
        public DbDataType DataType { get; internal set; }
        public DefaultValue DefaultValue { get; internal set; }
        public DbTable Table { get; }
        public NumberPrecision NumberPrecision { get; internal set; }
        public string MaxSize
        {
            get
            {
                if (DataType == DbDataType.Char && (!_maxSize.HasValue || _maxSize.Value <= 0))
                {
                    return "1";
                }
                if ((DataType == DbDataType.String || DataType == DbDataType.Varbinary) && (!_maxSize.HasValue || _maxSize.Value <= 0 || _maxSize.Value > 8000))
                {
                    return "max";
                }
                return _maxSize?.ToString();

            }
            internal set
            {
                if (value == null)
                {
                    _maxSize = null;
                }
                else
                {
                    int size;
                    int.TryParse(value, out size);
                    _maxSize = size;
                }
            }
        }
        public bool IsNullable { get; internal set; }
        public bool Unique { get; internal set; }
        internal DbRelationship ForeignkeyRelationship { get; set; }
        public bool IsPrimaryKey
        {
            get
            {
                return _isPrimaryKey;
            }
            set
            {
                var pk = Table.GetKey();
                if (pk != null)
                {
                    pk._isPrimaryKey = false;
                }
                _isPrimaryKey = value;
                IsNullable = false;
            }
        }
        public bool IsForeignKey => ForeignkeyRelationship != null;
        public ClassMemberInfo[] MappedMembers => _mappedMembers.ToArray();
        public bool IsMapped => _mappedMembers.Any();
        
        public bool IsMappedTo(ClassMemberInfo member)
        {
            return _mappedMembers.Any(r => r.Equals(member));
        }
        public bool IsMappedToMemberName(string memberName)
        {
            return _mappedMembers.Any(r => r.Name.Equals(memberName, StringComparison.Ordinal));
        }
        public bool ExcludeFromSelect { get; internal set; }
        public void MapToMember(ClassMemberInfo member)
        {
            _mappedMembers.Add(member);
        }
        public bool IsMappedToClass(Type entityType)
        {
            if (!_mappedMembers.Any()) return false;

            return _mappedMembers.Any(r => r.DeclaringClassType == entityType);
        }

        public override int GetHashCode()
        {
            return Table.TableName.GetHashCode() ^ ColumnName.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            var other = obj as DbColumn;

            return other != null && other.GetHashCode() == GetHashCode() && other.Table.TableName.Equals(Table.TableName);
        }
        public override string ToString()
        {
            return ColumnName;
        }


        
    }
}
