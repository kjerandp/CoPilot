using System;
using System.Linq;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config.Builders
{
    /// <summary>
    /// Non-generic table builder used for configuring table specific entities or settings
    /// </summary>
    public class TableBuilder : BaseBuilder
    {
        /// <summary>
        /// Representation of database table for the DbModel
        /// </summary>
        public DbTable Table { get; }

        public TableBuilder(DbModel model, DbTable table): base(model)
        {
            Table = table;
        }

        public TableBuilder(DbModel model, string schema, string tableName) : base(model)
        {
            Table = model.AddTable(tableName, schema);
        }

        /// <summary>
        /// Map a column
        /// </summary>
        /// <param name="columnName">Name of column
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <param name="alias">Alias for column that can be used when setting values using anonymous objects or unmapped POCOs</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder Column(string columnName, string alias = null)
        {
            var col = AddColumnIfNotExist(columnName, alias);
            SetDataType(col, DbDataType.Unknown);
            return new ColumnBuilder(Model, col);
        }

        /// <summary>
        /// Map a column
        /// </summary>
        /// <param name="columnName">Name of column
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <param name="dataType">Datatype of the column</param>
        /// <param name="alias">Alias for column that can be used when setting values using anonymous objects or unmapped POCOs</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder Column(string columnName, DbDataType dataType, string alias = null)
        {
            var col = AddColumnIfNotExist(columnName, alias);
            SetDataType(col, dataType);
            return new ColumnBuilder(Model, col);
        }

        /// <summary>
        /// Specify primary key for table
        /// </summary>
        /// <param name="columnName">Name of primary key column
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <param name="dataType">Primary key column datatype</param>
        /// <param name="alias">Alias for column that can be used when setting values using anonymous objects or unmapped POCOs</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder HasKey(string columnName, DbDataType dataType, string alias = null)
        {
            var pk = AddColumnIfNotExist(columnName, alias);
            SetDataType(pk, dataType);
            pk.IsPrimaryKey = true;
            return new ColumnBuilder(Model, pk);
        }
        
        /// <summary>
        /// Map a column (internal use)
        /// </summary>
        /// <param name="member"><see cref="ClassMemberInfo"/></param>
        /// <param name="columnName">Name of column
        /// <remarks>Column names can be prefixed with the tilde sign (~) to automatically add the table name as column name prefix</remarks>
        /// </param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        internal ColumnBuilder Column(ClassMemberInfo member, string columnName)
        {
            var col = AddColumnIfNotExist(member, columnName);
            return new ColumnBuilder(Model, col);
        }

        internal string TransformColumnName(string input)
        {
            return DbMapper.TransformColumnName(Table, input);
        }

        internal string GenerateColumnName(ClassMemberInfo member)
        {
            return Model.GenerateColumnName(Table, member);
        }

        protected DbColumn AddColumnIfNotExist(ClassMemberInfo member, string columnName = null, string alias = null)
        {
            var col = Table.GetColumn(member);

            if (col == null)
            {
                columnName = string.IsNullOrEmpty(columnName) ? GenerateColumnName(member) : TransformColumnName(columnName);
                col = AddColumnIfNotExist(columnName);
                if (col.DataType == DbDataType.Unknown)
                {
                    SetDataType(col, DbConversionHelper.GetDbDataType(member), member.MemberType.IsNullable());
                }
                col.MapToMember(member);
            }
            return col;
        }

        protected DbColumn AddColumnIfNotExist(string columnName, string alias = null)
        {
            if(string.IsNullOrEmpty(columnName)) throw new CoPilotConfigurationException("Column name can't be null!");
            columnName = TransformColumnName(columnName);
            var col = Table.GetColumnByName(columnName);

            if (col == null)
            {
                col = Table.AddColumn(columnName, DbDataType.Unknown, alias);
            }
            return col;
        }

        protected DbRelationship CreateRelationship(Type toEntityType, ClassMemberInfo foreignKeyMember, string foreignKeyName)
        {
            if (!Model.IsMapped(toEntityType))
            {
                throw new CoPilotConfigurationException($"The target type '{toEntityType.Name}' is not mapped to a table.");
            }

            var toTableMap = Model.GetTableMap(toEntityType);
            if(toTableMap == null) throw new CoPilotConfigurationException("The entity type to create the relationship to is not mapped!");

            var keys = toTableMap.Table.GetKeys();

            if (keys.Length > 1) throw new CoPilotUnsupportedException("Relationships to an entity with composite primary key is not supported!");
            
            var pkCol = keys.SingleOrDefault();
            
            if (pkCol == null)
                throw new CoPilotConfigurationException($"Table '{toTableMap.Table.TableName}' does not have a key defined.");

            if (!foreignKeyMember.MemberType.IsSimpleValueType()) throw new CoPilotUnsupportedException("Key members must be of a simple data type!");
            if (string.IsNullOrEmpty(foreignKeyName))
            {
                foreignKeyName = pkCol.ColumnName;
            }
            var fkCol = AddColumnIfNotExist(foreignKeyMember, foreignKeyName);

            if (fkCol.DataType == DbDataType.Unknown)
            {
                SetDataType(fkCol, foreignKeyMember.DataType, foreignKeyMember.MemberType.IsNullable());
            }
            var relationship = new DbRelationship(fkCol, pkCol);

            fkCol.ForeignkeyRelationship = relationship;

            toTableMap.Table.AddInverseRelationship(relationship);

            return relationship;
        }

        internal static void SetDataType(DbColumn col, DbDataType type, bool isFromNullableType = false)
        {
            if (col.DataType != type)
            {
                col.DataType = type;
                if (col.DataType == DbDataType.String && col.MaxSize == null)
                {
                    col.MaxSize = DbConversionHelper.DefaultDbStringSize;
                }
                if (!col.NullableExplicitSet && isFromNullableType)
                {
                    col.IsNullable = true;
                }
            }
            if (!DbConversionHelper.HasSize(col.DataType))
            {
                col.MaxSize = null;
            }
        }
    }
}