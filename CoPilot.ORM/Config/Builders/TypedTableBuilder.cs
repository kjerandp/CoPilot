using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config.Builders
{
    public class TableBuilder<T> : TableBuilder where T : class
    {
        private readonly TableMapEntry _map;

        public TableBuilder(DbModel model, TableMapEntry map) : base(model, map.Table)
        {
            _map = map;
        }
        public ColumnBuilder WithKey(Expression<Func<T, object>> prop, string columnName = null)
        {
            var member = ExpressionHelper.GetMemberInfoFromExpression(prop);

            if (!member.MemberType.IsSimpleValueType())
            {
                throw new ArgumentException("Invalid property type. Only simple data types may be used as primary key!");
            }
            
            var pk = AddColumnIfNotExist(member, columnName);
            
            pk.IsPrimaryKey = true;
            pk.IsNullable = false;
            pk.DefaultValue = DefaultValue.PrimaryKey;
            return new ColumnBuilder(Model, pk);
        }
        public ColumnBuilder WithKey(Expression<Func<T, object>> prop, string columnName, DefaultValue defaultValue)
        {
            var cb = WithKey(prop, columnName);
            cb.DefaultValue(defaultValue);
            return cb;
        }
        public ColumnBuilder Column(Expression<Func<T, object>> property, string columnName)
        {
            var member = ExpressionHelper.GetMemberInfoFromExpression(property);
            var col = AddColumnIfNotExist(member, columnName);
            if (col.DataType == DbDataType.Unknown)
            {
                SetDataType(col, DbConversionHelper.GetDbDataType(member));
            }
            return new ColumnBuilder(Model, col);
        }
        public ColumnBuilder Column(Expression<Func<T, object>> property, string columnName, int maxSize)
        {
            var builder = Column(property, columnName);
            builder.MaxSize(maxSize);
            return builder;
        }
        public ColumnBuilder Column(Expression<Func<T, object>> property, string columnName, NumberPrecision precision)
        {
            var builder = Column(property, columnName);
            builder.NumberPrecision(precision);
            return builder;
        }
        public ColumnBuilder Column(Expression<Func<T, object>> property, string columnName, string lookupTable, string lookupColumn = null)
        {
            if (Model.Tables.All(r => r.TableName != lookupTable)) throw new ArgumentException("Lookup table not defined!");
            var lTable = Model.Tables.Single(r => r.TableName == lookupTable);
            var builder = Column(property, columnName);
            builder.DataType(lTable.GetKey().DataType);
            return builder.LookupTable(lTable, lookupColumn);
        }
        public ColumnBuilder Column(Expression<Func<T, object>> property)
        {
            var member = ExpressionHelper.GetMemberInfoFromExpression(property);
            var col = AddColumnIfNotExist(member);
            if (col.DataType == DbDataType.Unknown)
            {
                SetDataType(col, DbConversionHelper.GetDbDataType(member));
            }
            return new ColumnBuilder(Model, col);
        }

        public TableBuilder<T> Ignore(params Expression<Func<T, object>>[] property)
        {
            foreach (var propEx in property)
            {
                var prop = ExpressionHelper.GetMemberInfoFromExpression(propEx);
                Model.Ignored.Add(prop);
            }
            
            return this;
        }
        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, TTo>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            if (!Model.IsMapped<TTo>())
            {
                throw new ArgumentException($"The target type '{typeof(TTo).Name}' is not mapped to a table.");
            }

            var toTableMap = Model.GetTableMap<TTo>();
            var pkCol = toTableMap.Table.GetKey();


            if (pkCol == null)
                throw new ArgumentException($"Table '{toTableMap.Table.TableName}' does not have a key defined.");

            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);


            if (string.IsNullOrEmpty(foreignKeyName))
            {
                foreignKeyName = toTableMap.Table.GetKey().ColumnName;
            } 

            var fkCol = AddColumnIfNotExist(foreignKeyName);
            if (fkCol.DataType == DbDataType.Unknown)
            {
                SetDataType(fkCol, foreignKeyDataType ?? DbDataType.Int32);
            }
            var fkFor = keyMemberInfo;

            var relationship = new DbRelationship(fkCol, pkCol);
            _map.MapMemberToRelationship(fkFor, relationship);

            fkCol.ForeignkeyRelationship = relationship;

            toTableMap.Table.AddInverseRelationship(relationship);

            return new RelationshipBuilder<T, TTo>(Model, relationship);
        }

        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, int>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);
            return HasOne<TTo>(keyMemberInfo, foreignKeyName, foreignKeyDataType);
        }

        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, int?>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);
            return HasOne<TTo>(keyMemberInfo, foreignKeyName, foreignKeyDataType);
        }


        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, string>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);
            return HasOne<TTo>(keyMemberInfo, foreignKeyName, foreignKeyDataType);
        }

        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, Guid>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);
            return HasOne<TTo>(keyMemberInfo, foreignKeyName, foreignKeyDataType);
        }

        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, Guid?>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);
            return HasOne<TTo>(keyMemberInfo, foreignKeyName, foreignKeyDataType);
        }

        private RelationshipBuilder<T, TTo> HasOne<TTo>(ClassMemberInfo keyMemberInfo, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var relationship = CreateRelationship(typeof(TTo), keyMemberInfo, foreignKeyName);
            relationship.ForeignKeyColumn.IsNullable = keyMemberInfo.MemberType.IsNullable();
            SetDataType(relationship.ForeignKeyColumn, foreignKeyDataType ?? keyMemberInfo.DataType);

            return new RelationshipBuilder<T, TTo>(Model, relationship);
        }

        public RelationshipBuilder<TFrom, T> HasMany<TFrom>(Expression<Func<T, ICollection<TFrom>>> collection, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TFrom : class 
        {
            if (!Model.IsMapped<TFrom>())
            {
                throw new ArgumentException($"The source type '{typeof(TFrom).Name}' is not mapped to a table.");
            }
            if (collection == null)
            {
                throw new ArgumentException("A One-To-Many relationship must be mapped to a collection in the declaring entity!");
            }
            var pkCol = Table.GetKey();
            if (pkCol == null)
                throw new ArgumentException($"Table '{Table.TableName}' does not have a key defined.");

            if (string.IsNullOrEmpty(foreignKeyName))
            {
                foreignKeyName = Table.GetKey().ColumnName;
            }
            var inverseKey = ExpressionHelper.GetMemberInfoFromExpression(collection);
            
            var fromTableMap = Model.GetTableMap<TFrom>();
            var fkCol = fromTableMap.Table.GetColumnByName(foreignKeyName);

            if (fkCol == null)
            {
                fkCol = fromTableMap.Table.AddColumn(DbMapper.TransformColumnName(fromTableMap.Table, foreignKeyName), foreignKeyDataType ?? DbDataType.Int32);
            }

            var relationship = new DbRelationship(fkCol, pkCol);


            _map.MapMemberToRelationship(inverseKey, relationship);
            
            fkCol.ForeignkeyRelationship = relationship;
            Table.AddInverseRelationship(relationship);

            return new RelationshipBuilder<TFrom, T>(Model, relationship);
        }

        public TableBuilder<T> SetValueAdapter<TObj>(Expression<Func<T, TObj>> field, Func<TObj, object> toDb = null, Func<object, TObj> fromDb = null)
        {
            var prop = ExpressionHelper.GetMemberInfoFromExpression(field);
            AddColumnIfNotExist(prop);

            _map.SetValueAdapter(prop, toDb, fromDb);
            return this;
        }

        public TableBuilder<T> SetValueAdapter(Expression<Func<T, object>> field, ValueAdapter adapter)
        {
            var prop = ExpressionHelper.GetMemberInfoFromExpression(field);

            AddColumnIfNotExist(prop);

            _map.SetValueAdapter(prop, adapter);
            
            return this;
        }

        public TableBuilder<T> Operations(OperationType operations)
        {
            _map.Operations = operations;
            return this;
        }
    }
}