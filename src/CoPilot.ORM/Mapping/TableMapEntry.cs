using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Config;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using System.Reflection;

namespace CoPilot.ORM.Mapping
{
    public class TableMapEntry
    {
        public TableMapEntry(Type entityType, DbTable table, OperationType operations = OperationType.Select)
        {
            EntityType = entityType;
            Table = table;
            Operations = operations;
        }

        public Type EntityType { get; }
        public DbTable Table { get; }
        public OperationType Operations { get; internal set; }

        internal Dictionary<ClassMemberInfo, DbRelationship> MemberToRelationshipMappings = new Dictionary<ClassMemberInfo, DbRelationship>();
        internal Dictionary<ClassMemberInfo, ValueAdapter> ValueAdapters = new Dictionary<ClassMemberInfo, ValueAdapter>();

        public ClassMemberInfo GetMemberByName(string name)
        {
            var member = ClassMemberInfo.Create(PathHelper.GetMemberFromPath(EntityType, name));
            return member;
        }

        public void MapMemberToRelationship(ClassMemberInfo member, DbRelationship relationship)
        {
            if (MemberToRelationshipMappings.ContainsKey(member))
            {
                throw new ArgumentException("Member is already mapped to a relationship!");
            }
            MemberToRelationshipMappings.Add(member, relationship);
        }

        public ClassMemberInfo GetKeyForMember(DbRelationship relationship)
        {
            var rel = MemberToRelationshipMappings.SingleOrDefault(r => r.Value.Equals(relationship) && r.Key.DeclaringClassType.GetTypeInfo().IsAssignableFrom(EntityType));
            return rel.Value != null ? rel.Key : null;
        }

        public DbRelationship GetRelationshipByMember(ClassMemberInfo member)
        {
            var m = MemberToRelationshipMappings.SingleOrDefault(
                    r => r.Key.Name == member.Name && member.DeclaringClassType.GetTypeInfo().IsAssignableFrom(r.Key.DeclaringClassType));

            if (m.Value == null)
            {
                throw new ArgumentException("Oh dear!");
            }

            return m.Value;
        }

        public DbColumn GetColumnByMember(ClassMemberInfo member)
        {
            return Table.GetColumnByMember(member);
        }

        public void SetValueOnMappedMember(ClassMemberInfo member, object instance, object value)
        {
            if (ValueAdapters.ContainsKey(member))
            {
                var adapter = ValueAdapters[member];
                value = adapter(MappingTarget.Object, value);
            }
            member.SetValue(instance, value);
        }

        public override int GetHashCode()
        {
            return EntityType.GetHashCode() ^ Table.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return GetHashCode() == obj?.GetHashCode();    
        }

        public override string ToString()
        {
            return $"{EntityType.Name} > {Table.TableName}";
        }

        public void SetValueAdapter<TObj>(ClassMemberInfo prop, Func<TObj, object> toDb, Func<object, TObj> fromDb) 
        {
            ValueAdapter adapter = (target, value) =>
            {
                if (value == null) return null;

                if (target == MappingTarget.Database)
                {
                    return toDb != null ? toDb.Invoke((TObj)value) : value;
                }
                return fromDb != null ? fromDb.Invoke(value) : value;
            };

            SetValueAdapter(prop, adapter);
        }

        public void SetValueAdapter(ClassMemberInfo prop, ValueAdapter adapter)
        {
            if (ValueAdapters.ContainsKey(prop))
            {
                ValueAdapters[prop] = adapter;
            }
            else
            {
                ValueAdapters.Add(prop, adapter);
            }
        }

        public ValueAdapter GetAdapter(ClassMemberInfo member)
        {
            if (ValueAdapters.ContainsKey(member))
            {
                return ValueAdapters[member];
            }
            return null;
        }

        public ClassMemberInfo GetMappedMember(DbColumn col)
        {
            return col.MappedMembers.SingleOrDefault(r => r.DeclaringClassType.GetTypeInfo().IsAssignableFrom(EntityType));
        }

        public object GetValueForColumn(object instance, DbColumn column, bool convertToDbType = false)
        {
            var member = GetMappedMember(column);
            if (member == null && column.IsForeignKey)
            {
                var keyFor = GetKeyForMember(column.ForeignkeyRelationship);
                var keyForInstance = keyFor?.GetValue(instance);
                if (keyForInstance != null)
                {
                    var pkM = column.ForeignkeyRelationship.PrimaryKeyColumn.MappedMembers.SingleOrDefault(r => r.DeclaringClassType.IsInstanceOfType(keyForInstance));
                    return pkM?.GetValue(keyForInstance);
                }
            }
            
            object val;
            if (convertToDbType)
            {
                ReflectionHelper.ConvertValueToType(DbConversionHelper.MapDbToRuntimeDataType(column.DataType), member?.GetValue(instance), out val, false);
            }
            else
            {
                val = member?.GetValue(instance);
            }
            return val;
        }

        public bool SetValueForColumn(object instance, DbColumn column, object value)
        {
            var member = GetMappedMember(column);
            if (member == null)
            {
                if (column.IsForeignKey)
                {
                    var keyFor = GetKeyForMember(column.ForeignkeyRelationship);
                    if (keyFor == null) return false;
                    var keyForInstance = keyFor.GetValue(instance);
                    if (keyForInstance == null)
                    {
                        keyForInstance = ReflectionHelper.CreateInstance(keyFor.MemberType);
                        keyFor.SetValue(instance, keyForInstance);
                    }
                    var pkM = column.ForeignkeyRelationship.PrimaryKeyColumn.MappedMembers.SingleOrDefault(r => r.DeclaringClassType.IsInstanceOfType(keyForInstance));
                    if (pkM != null)
                    {
                        pkM.SetValue(keyForInstance, value);
                        return true;
                    }
                }
                return false;
            }
            member.SetValue(instance, value);
            return true;
        }

        internal ValueAdapter GetAdapter(DbColumn column)
        {
            var member = GetMappedMember(column);
            return member == null ? null : GetAdapter(member);
        }

        public Dictionary<DbColumn, ClassMemberInfo> GetMappedColumns()
        {
            var allColumns = Table.Columns;
            var mappedColumns = allColumns
                .SelectMany(r => r.MappedMembers
                    .Where(m => m.DeclaringClassType.GetTypeInfo().IsAssignableFrom(EntityType)), (column, info) => new {column, info })
                    .ToDictionary(k => k.column, v => v.info);

            var missingColumns = allColumns.Except(mappedColumns.Keys);

            foreach (var missingColumn in missingColumns)
            {
                mappedColumns.Add(missingColumn,null);
            }    
                
            return mappedColumns;
        }
    }

    
}
