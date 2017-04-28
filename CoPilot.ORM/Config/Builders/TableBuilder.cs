using System;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config.Builders
{
    public class TableBuilder : BaseBuilder
    {
        public DbTable Table { get; }

        public TableBuilder(DbModel model, DbTable table): base(model)
        {
            Table = table;
        }

        public TableBuilder(DbModel model, string schema, string tableName) : base(model)
        {
            Table = model.AddTable(tableName, schema);
        }

        public ColumnBuilder Column(string columnName, DbDataType dataType, string alias = null)
        {
            var col = AddColumnIfNotExist(columnName, alias);
            SetDataType(col, dataType);
            return new ColumnBuilder(Model, col);
        }

        public ColumnBuilder Column(ClassMemberInfo member, string columnName)
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
            return DbMapper.GenerateColumnName(Table, member);
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
                    SetDataType(col, DbConversionHelper.GetDbDataType(member));
                }
                col.MapToMember(member);
            }
            return col;
        }

        protected DbColumn AddColumnIfNotExist(string columnName, string alias = null)
        {
            if(string.IsNullOrEmpty(columnName)) throw new ArgumentException("Column name can't be null!");
            columnName = TransformColumnName(columnName);
            var col = Table.GetColumnByName(columnName);

            if (col == null)
            {
                col = Table.AddColumn(columnName, DbDataType.Unknown, alias);
            }
            return col;
        }

        public ColumnBuilder HasKey(string columnName, DbDataType dataType, string alias = null)
        {
            var pk = AddColumnIfNotExist(columnName, alias);
            SetDataType(pk, dataType);
            pk.IsPrimaryKey = true;
            return new ColumnBuilder(Model, pk);
        }

        protected DbRelationship CreateRelationship(Type toEntityType, ClassMemberInfo foreignKeyMember, string foreignKeyName)
        {
            if (!Model.IsMapped(toEntityType))
            {
                throw new ArgumentException($"The target type '{toEntityType.Name}' is not mapped to a table.");
            }

            var toTableMap = Model.GetTableMap(toEntityType);
            if(toTableMap == null) throw new ArgumentException("The entity type to create the relationship to is not mapped!");

            var pkCol = toTableMap.Table.GetKey();
            
            if (pkCol == null)
                throw new ArgumentException($"Table '{toTableMap.Table.TableName}' does not have a key defined.");

            if (!foreignKeyMember.MemberType.IsSimpleValueType()) throw new ArgumentException("Weird!");
            if (string.IsNullOrEmpty(foreignKeyName))
            {
                foreignKeyName = pkCol.ColumnName;
            }
            var fkCol = AddColumnIfNotExist(foreignKeyMember, foreignKeyName);

            if (fkCol.DataType == DbDataType.Unknown)
            {
                SetDataType(fkCol, foreignKeyMember.DataType);
            }
            var relationship = new DbRelationship(fkCol, pkCol);

            fkCol.ForeignkeyRelationship = relationship;

            toTableMap.Table.AddInverseRelationship(relationship);

            return relationship;
        }

        internal static void SetDataType(DbColumn col, DbDataType type)
        {
            if (col.DataType != type)
            {
                col.DataType = type;
                if (col.DataType == DbDataType.String && col.MaxSize == null)
                {
                    col.MaxSize = DbConversionHelper.DefaultDbStringSize;
                } 
            }
            if (!DbConversionHelper.HasSize(col.DataType))
            {
                col.MaxSize = null;
            }
        }
    }
}