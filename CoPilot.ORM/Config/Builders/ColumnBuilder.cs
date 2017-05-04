using System;
using System.Linq;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config.Builders
{
    public class ColumnBuilder : BaseBuilder
    {
        private readonly DbColumn _column;

        public ColumnBuilder(DbModel model, DbColumn column) : base(model)
        {
            _column = column;
        }


        public ColumnBuilder DefaultValue(DefaultValue defaultValue)
        {
            _column.DefaultValue = defaultValue;
            return this;
        }

        public ColumnBuilder DefaultValue(object value)
        {
            _column.DefaultValue = value != null ? new DefaultValue(DbExpressionType.Constant, value) : null;
            return this;
        }

        public ColumnBuilder DataType(DbDataType dataType)
        {
            TableBuilder.SetDataType(_column, dataType);
            return this;
        }

        public ColumnBuilder MaxSize(int? value = null)
        {
            _column.MaxSize = value?.ToString();
            return this;
        }

        public ColumnBuilder NumberPrecision(NumberPrecision precision)
        {
            _column.NumberPrecision = precision;
            
            return this;
        }

        public ColumnBuilder NumberPrecision(int precision, int scale)
        {
            return NumberPrecision(new NumberPrecision(precision, scale));
        }

        public ColumnBuilder IsNullable()
        {
            _column.IsNullable = true;
            _column.NullableExplicitSet = true;
            return this;
        }

        public ColumnBuilder Unique()
        {
            _column.IsNullable = false;
            _column.Unique = true;
            return this;
        }

        public ColumnBuilder IsRequired()
        {
            _column.IsNullable = false;
            _column.NullableExplicitSet = true;
            return this;
        }

        public ColumnBuilder IsKey()
        {
            _column.IsPrimaryKey = true;
            return this;
        }
        public ColumnBuilder Alias(string aliasName)
        {
            if (_column.Table.Columns.Any(r => r.AliasName.Equals(aliasName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Alias already used for another column!");
            }
            _column.AliasName = aliasName;
            return this;
        }

        public ColumnBuilder LookupTable(DbTable lookupTable, string lookupColumnName)
        {
            var lookupColumn = string.IsNullOrEmpty(lookupColumnName)
                ? lookupTable.Columns.FirstOrDefault(r => !r.IsPrimaryKey)
                : lookupTable.GetColumnByName(lookupColumnName, StringComparison.OrdinalIgnoreCase);
            if (lookupColumn == null) throw new ArgumentException("No lookup column configured");

            var relationship = new DbRelationship(_column, lookupTable.GetSingularKey()) {LookupColumn = lookupColumn};
            _column.ForeignkeyRelationship = relationship;
            return this;
        }

        
    }
}