using System;
using System.Linq;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config.Builders
{
    /// <summary>
    /// Builder class for setting column specific configurations
    /// </summary>
    public class ColumnBuilder : BaseBuilder
    {
        private readonly DbColumn _column;

        public ColumnBuilder(DbModel model, DbColumn column) : base(model)
        {
            _column = column;
        }

        /// <summary>
        /// Set a default value (database level) for column <see cref="DataTypes.DefaultValue"/> <seealso cref="DbExpressionType"/>
        /// </summary>
        /// <param name="defaultValue">Default value config</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder DefaultValue(DefaultValue defaultValue)
        {
            _column.DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// Set a specific default value (database level) for column <see cref="DataTypes.DefaultValue"/> <seealso cref="DbExpressionType"/>
        /// </summary>
        /// <param name="value">Default value</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder DefaultValue(object value)
        {
            _column.DefaultValue = value != null ? new DefaultValue(DbExpressionType.Constant, value) : null;
            return this;
        }

        /// <summary>
        /// Explicitly set the column datatype 
        /// </summary>
        /// <param name="dataType">Column datatype</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder DataType(DbDataType dataType)
        {
            TableBuilder.SetDataType(_column, dataType);
            return this;
        }

        /// <summary>
        /// Set the max size for column with datatype that has a max size property
        /// </summary>
        /// <param name="value">Max size - omit or set to null for maximum</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder MaxSize(int? value = null)
        {
            _column.MaxSize = value?.ToString();
            return this;
        }

        /// <summary>
        /// Set the number precision for column that are of type Number
        /// </summary>
        /// <param name="precision"><see cref="DataTypes.NumberPrecision"/></param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder NumberPrecision(NumberPrecision precision)
        {
            _column.NumberPrecision = precision;
            
            return this;
        }

        /// <summary>
        /// Set the number precision for column that are of type Number
        /// </summary>
        /// <param name="precision">Precision</param>
        /// <param name="scale">Scale</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder NumberPrecision(int precision, int scale)
        {
            return NumberPrecision(new NumberPrecision(precision, scale));
        }

        /// <summary>
        /// Explicitly set column as NULLABLE
        /// </summary>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder IsNullable()
        {
            _column.IsNullable = true;
            _column.NullableExplicitSet = true;
            return this;
        }

        /// <summary>
        /// Add an unique index constraint to column
        /// </summary>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder Unique()
        {
            _column.IsNullable = false;
            _column.Unique = true;
            return this;
        }

        /// <summary>
        /// Explicitly set column as NOT NULLABLE
        /// </summary>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder IsRequired()
        {
            _column.IsNullable = false;
            _column.NullableExplicitSet = true;
            return this;
        }

        /// <summary>
        /// Indicate that the column is a primary key
        /// </summary>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder IsKey()
        {
            _column.IsPrimaryKey = true;
            return this;
        }

        /// <summary>
        /// Give the column an alias name. Useful for columns that are not mapped to a specific member.
        /// </summary>
        /// <returns>Column builder for chaining column specific configurations</returns>
        public ColumnBuilder Alias(string aliasName)
        {
            if (_column.Table.Columns.Any(r => r.AliasName.Equals(aliasName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Alias already used for another column!");
            }
            _column.AliasName = aliasName;
            return this;
        }

        /// <summary>
        /// Configure a lookup table <see cref="DbTable"/> <seealso cref="DbRelationship"/>
        /// </summary>
        /// <param name="lookupTable">Name of lookup table</param>
        /// <param name="lookupColumnName">Name of the lookup column</param>
        /// <returns>Column builder for chaining column specific configurations</returns>
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