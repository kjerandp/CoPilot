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
    /// <summary>
    /// Map a POCO class to a database table
    /// </summary>
    /// <typeparam name="T">Type of POCO</typeparam>
    public class TableBuilder<T> : TableBuilder where T : class
    {
        private readonly TableMapEntry _map;

        public TableBuilder(DbModel model, TableMapEntry map) : base(model, map.Table)
        {
            _map = map;
        }

        /// <summary>
        /// Add primary key mapping for table
        /// </summary>
        /// <param name="prop">Expression to select property mapped to primary key</param>
        /// <param name="columnName">Name of primary key column
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder AddKey(Expression<Func<T, object>> prop, string columnName = null)
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

        /// <summary>
        /// Add primary key mapping for table
        /// </summary>
        /// <param name="prop">Expression to select property mapped to primary key</param>
        /// <param name="columnName">Name of primary key column
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <param name="defaultValue">Set a default value (database level) for column <see cref="DefaultValue"/> <seealso cref="DbExpressionType"/></param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder AddKey(Expression<Func<T, object>> prop, string columnName, DefaultValue defaultValue)
        {
            var cb = AddKey(prop, columnName);
            cb.DefaultValue(defaultValue);
            return cb;
        }

        /// <summary>
        /// Add property to column mapping for table
        /// </summary>
        /// <param name="property">Expression to select property to be mapped</param>
        /// <param name="columnName">Name of database column
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder Column(Expression<Func<T, object>> property, string columnName)
        {
            var member = ExpressionHelper.GetMemberInfoFromExpression(property);
            var col = AddColumnIfNotExist(member, columnName);
            if (col.DataType == DbDataType.Unknown)
            {
                SetDataType(col, DbConversionHelper.GetDbDataType(member), member.MemberType.IsNullable());
            }
            return new ColumnBuilder(Model, col);
        }

        /// <summary>
        /// Add property to column mapping for table
        /// </summary>
        /// <param name="property">Expression to select property to be mapped</param>
        /// <param name="columnName">Name of database column
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <param name="maxSize">Specify max size if applicable to database type</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder Column(Expression<Func<T, object>> property, string columnName, int maxSize)
        {
            var builder = Column(property, columnName);
            builder.MaxSize(maxSize);
            return builder;
        }

        /// <summary>
        /// Add property to column mapping for table
        /// </summary>
        /// <param name="property">Expression to select property to be mapped</param>
        /// <param name="columnName">Name of database column
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <param name="precision">Specify number precision if applicable to database type</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder Column(Expression<Func<T, object>> property, string columnName, NumberPrecision precision)
        {
            var builder = Column(property, columnName);
            builder.NumberPrecision(precision);
            return builder;
        }

        /// <summary>
        /// Add property to column mapping for table and connect it to a lookup table
        /// </summary>
        /// <param name="property">Expression to select property to be mapped</param>
        /// <param name="columnName">Name of database column
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <param name="lookupTable">Name of lookup table <see cref="DbTable"/> <seealso cref="DbRelationship"/></param>
        /// <param name="lookupColumn">Name of the lookup column</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder Column(Expression<Func<T, object>> property, string columnName, string lookupTable, string lookupColumn = null)
        {
            if (Model.Tables.All(r => r.TableName != lookupTable)) throw new ArgumentException("Lookup table not defined!");
            var lTable = Model.Tables.Single(r => r.TableName == lookupTable);
            var builder = Column(property, columnName);
            builder.DataType(lTable.GetSingularKey().DataType);
            return builder.LookupTable(lTable, lookupColumn);
        }

        /// <summary>
        /// Access column builder for property
        /// </summary>
        /// <param name="property">Expression to select property to be mapped</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder Column(Expression<Func<T, object>> property)
        {
            var member = ExpressionHelper.GetMemberInfoFromExpression(property);
            var col = AddColumnIfNotExist(member);
            if (col.DataType == DbDataType.Unknown)
            {
                SetDataType(col, DbConversionHelper.GetDbDataType(member), member.MemberType.IsNullable());
            }
            return new ColumnBuilder(Model, col);
        }

        /// <summary>
        /// Ignore a property in the POCO
        /// </summary>
        /// <param name="property">Expression to select property to be ignored</param>
        /// <returns>Table builder for chaining table specific configurations</returns>
        public TableBuilder<T> Ignore(params Expression<Func<T, object>>[] property)
        {
            foreach (var propEx in property)
            {
                var prop = ExpressionHelper.GetMemberInfoFromExpression(propEx);
                Model.Ignored.Add(prop);
            }
            
            return this;
        }

        /// <summary>
        /// Define a Many-To-One relationship <remarks>The related entity must be mapped</remarks>
        /// </summary>
        /// <typeparam name="TTo">POCO mapped to the related table</typeparam>
        /// <param name="foreignKeyMember">Expression to select property to represent the foreign key relationship 
        /// <remarks>If there is a property that matches the foreign key column in the table, then use that property to specify the relationship instead, then define the navigation property as a KeyForMember instead</remarks>
        /// </param>
        /// <param name="foreignKeyName">Column name of foreign key</param>
        /// <param name="foreignKeyDataType">Datatype of foreign key <remarks>Should match datatype of referenced PK column</remarks></param>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, TTo>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            if (!Model.IsMapped<TTo>())
            {
                throw new ArgumentException($"The target type '{typeof(TTo).Name}' is not mapped to a table.");
            }

            var toTableMap = Model.GetTableMap<TTo>();
            var keys = toTableMap.Table.GetKeys();

            if (keys.Length != 1) throw new NotSupportedException("Relationships to an entity with composite primary key or no key is not supported!");
            var pkCol = keys.Single();


            if (pkCol == null)
                throw new ArgumentException($"Table '{toTableMap.Table.TableName}' does not have a key defined.");

            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);


            if (string.IsNullOrEmpty(foreignKeyName))
            {
                foreignKeyName = pkCol.ColumnName;
            } 

            var fkCol = AddColumnIfNotExist(foreignKeyName);
            if (fkCol.DataType == DbDataType.Unknown)
            {
                SetDataType(fkCol, foreignKeyDataType ?? pkCol.DataType, keyMemberInfo.MemberType.IsSimpleValueType() && keyMemberInfo.MemberType.IsNullable());
                if (pkCol.DataType == fkCol.DataType) //should be
                {
                    fkCol.MaxSize = pkCol.MaxSize;
                    fkCol.NumberPrecision = pkCol.NumberPrecision;
                }
            }
            var fkFor = keyMemberInfo;

            var relationship = new DbRelationship(fkCol, pkCol);
            _map.MapMemberToRelationship(fkFor, relationship);

            fkCol.ForeignkeyRelationship = relationship;

            toTableMap.Table.AddInverseRelationship(relationship);

            return new RelationshipBuilder<T, TTo>(Model, relationship);
        }

        /// <summary>
        /// Define a Many-To-One relationship <remarks>The related entity must be mapped</remarks>
        /// </summary>
        /// <typeparam name="TTo">POCO mapped to the related table</typeparam>
        /// <param name="foreignKeyMember">Expression to select property matching the foreign key column in the table</param>
        /// <param name="foreignKeyName">Column name of foreign key</param>
        /// <param name="foreignKeyDataType">Datatype of foreign key <remarks>Should match datatype of referenced PK column</remarks></param>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, int>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);
            return HasOne<TTo>(keyMemberInfo, foreignKeyName, foreignKeyDataType);
        }

        /// <summary>
        /// Define a Many-To-One relationship <remarks>The related entity must be mapped</remarks>
        /// </summary>
        /// <typeparam name="TTo">POCO mapped to the related table</typeparam>
        /// <param name="foreignKeyMember">Expression to select property matching the foreign key column in the table</param>
        /// <param name="foreignKeyName">Column name of foreign key</param>
        /// <param name="foreignKeyDataType">Datatype of foreign key <remarks>Should match datatype of referenced PK column</remarks></param>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, int?>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);
            return HasOne<TTo>(keyMemberInfo, foreignKeyName, foreignKeyDataType);
        }

        /// <summary>
        /// Define a Many-To-One relationship <remarks>The related entity must be mapped</remarks>
        /// </summary>
        /// <typeparam name="TTo">POCO mapped to the related table</typeparam>
        /// <param name="foreignKeyMember">Expression to select property matching the foreign key column in the table</param>
        /// <param name="foreignKeyName">Column name of foreign key</param>
        /// <param name="foreignKeyDataType">Datatype of foreign key <remarks>Should match datatype of referenced PK column</remarks></param>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, string>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);
            return HasOne<TTo>(keyMemberInfo, foreignKeyName, foreignKeyDataType);
        }

        /// <summary>
        /// Define a Many-To-One relationship <remarks>The related entity must be mapped</remarks>
        /// </summary>
        /// <typeparam name="TTo">POCO mapped to the related table</typeparam>
        /// <param name="foreignKeyMember">Expression to select property matching the foreign key column in the table</param>
        /// <param name="foreignKeyName">Column name of foreign key</param>
        /// <param name="foreignKeyDataType">Datatype of foreign key <remarks>Should match datatype of referenced PK column</remarks></param>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, Guid>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);
            return HasOne<TTo>(keyMemberInfo, foreignKeyName, foreignKeyDataType);
        }

        /// <summary>
        /// Define a Many-To-One relationship <remarks>The related entity must be mapped</remarks>
        /// </summary>
        /// <typeparam name="TTo">POCO mapped to the related table</typeparam>
        /// <param name="foreignKeyMember">Expression to select property matching the foreign key column in the table</param>
        /// <param name="foreignKeyName">Column name of foreign key</param>
        /// <param name="foreignKeyDataType">Datatype of foreign key <remarks>Should match datatype of referenced PK column</remarks></param>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
        public RelationshipBuilder<T, TTo> HasOne<TTo>(Expression<Func<T, Guid?>> foreignKeyMember, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var keyMemberInfo = ExpressionHelper.GetMemberInfoFromExpression(foreignKeyMember);
            return HasOne<TTo>(keyMemberInfo, foreignKeyName, foreignKeyDataType);
        }

        private RelationshipBuilder<T, TTo> HasOne<TTo>(ClassMemberInfo keyMemberInfo, string foreignKeyName = null, DbDataType? foreignKeyDataType = null) where TTo : class
        {
            var relationship = CreateRelationship(typeof(TTo), keyMemberInfo, foreignKeyName);
            relationship.ForeignKeyColumn.IsNullable = keyMemberInfo.MemberType.IsNullable();
            SetDataType(relationship.ForeignKeyColumn, foreignKeyDataType ?? keyMemberInfo.DataType, keyMemberInfo.MemberType.IsNullable());

            return new RelationshipBuilder<T, TTo>(Model, relationship);
        }

        /// <summary>
        /// Define a One-To-Many relationship <remarks>The related entity must be mapped. If possible it is recommended to specify the relationship from the entity holding the foreign key and use the InverseKeyMember to map it to a collection in the parent entitiy</remarks>
        /// </summary>
        /// <typeparam name="TFrom">POCO mapped to the related table</typeparam>
        /// <param name="collection">Expression to select property matching the collection used to navigate to the child entities</param>
        /// <param name="foreignKeyName">Column name of foreign key in related table</param>
        /// <param name="foreignKeyDataType">Datatype of foreign key in the related table</param>
        /// <returns>Relationship builder to chain relation specific configurations</returns>
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

            var keys = Table.GetKeys();
            if (keys.Length != 1) throw new NotSupportedException("Relationships to an entity with composite primary key or no key is not supported!");
            var pkCol = keys.Single();

            if (pkCol == null)
                throw new ArgumentException($"Table '{Table.TableName}' does not have a key defined.");

            if (string.IsNullOrEmpty(foreignKeyName))
            {
                foreignKeyName = pkCol.ColumnName;
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

        /// <summary>
        /// Allow you to set an adapter that can be used to transform values when getting and setting values to and from the database <see cref="ValueAdapter"/>
        /// </summary>
        /// <typeparam name="TObj">Type of the property in the POCO to transform to/from</typeparam>
        /// <param name="field">Expression to select property to apply the adapter to</param>
        /// <param name="toDb">Transformation to apply to value before sending it to the db</param>
        /// <param name="fromDb">Transformation to apply to value when receiving it from the db</param>
        /// <returns>Table builder for chaining table specific configurations</returns>
        public TableBuilder<T> SetValueAdapter<TObj>(Expression<Func<T, TObj>> field, Func<TObj, object> toDb = null, Func<object, TObj> fromDb = null)
        {
            var prop = ExpressionHelper.GetMemberInfoFromExpression(field);
            AddColumnIfNotExist(prop);

            _map.SetValueAdapter(prop, toDb, fromDb);
            return this;
        }

        /// <summary>
        /// Allow you to set an adapter that can be used to transform values when getting and setting values to and from the database <see cref="ValueAdapter"/>
        /// </summary>
        /// <param name="field">Expression to select property to apply the adapter to</param>
        /// <param name="adapter">ValueAdapter delegate to handle transformations <see cref="ValueAdapter"/></param>
        /// <returns>Table builder for chaining table specific configurations</returns>
        public TableBuilder<T> SetValueAdapter(Expression<Func<T, object>> field, ValueAdapter adapter)
        {
            var prop = ExpressionHelper.GetMemberInfoFromExpression(field);

            AddColumnIfNotExist(prop);

            _map.SetValueAdapter(prop, adapter);
            
            return this;
        }

        /// <summary>
        /// Specify what table operations to allow for this entity (INSERT, UPDATE, DELETE or ALL)
        /// </summary>
        /// <param name="operations">Operations to allow</param>
        /// <returns>Table builder for chaining table specific configurations</returns>
        public TableBuilder<T> Operations(OperationType operations)
        {
            _map.Operations = operations;
            return this;
        }

        
    }
}