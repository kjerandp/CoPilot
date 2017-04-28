using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public class SqlCreateStatementWriter : ICreateStatementWriter
    {
        public SqlStatement GetStatement(DbTable table, CreateOptions options)
        {
            var stm = new SqlStatement();
            stm.Script.Add($"CREATE TABLE [{table.Schema}].[{table.TableName}] (");
            var createColumns = new ScriptBlock();
            foreach (var dbColumn in table.Columns)
            {
                var extendedInfo = "";
                if (dbColumn.IsPrimaryKey)
                {
                    extendedInfo = " " + GetPrimaryKeyString(dbColumn, options);
                }
                else if (dbColumn.IsForeignKey)
                {
                    extendedInfo = " " + GetForeignKeyString(dbColumn);
                }
                createColumns.Add($"{(createColumns.ItemCount > 0 ? "," : "")}{dbColumn.ColumnName}{GetDataTypeString(dbColumn, options)}{extendedInfo}");
            }

            var uniqueColumns = table.Columns.Where(r => r.Unique);

            foreach (var uniqueColumn in uniqueColumns)
            {
                createColumns.Add($",CONSTRAINT UQ_{uniqueColumn.ColumnName} UNIQUE({uniqueColumn.ColumnName})");
            }

            stm.Script.Add(createColumns);
            stm.Script.Add(")");
            return stm;
        }

        private static string GetPrimaryKeyString(DbColumn column, CreateOptions options)
        {
            var str = string.Empty;
            if ((column.DefaultValue?.Expression == DbExpressionType.PrimaryKeySequence) || options.UseSequenceForPrimaryKeys)
            {
                if (column.DefaultValue?.Value == null)
                {
                    str += $"IDENTITY({options.KeySequenceStartAt},{options.KeySequenceIncrementBy})";
                }
                else
                {
                    str += column.DefaultValue.Value as string;
                }
                str += " ";
            }

            return str + "PRIMARY KEY";
        }

        private static string GetForeignKeyString(DbColumn column)
        {

            var str = $"REFERENCES {column.ForeignkeyRelationship.PrimaryKeyColumn.Table.TableName}({column.ForeignkeyRelationship.PrimaryKeyColumn.ColumnName})";
            return str;
        }

        private string GetDataTypeString(DbColumn column, CreateOptions options)
        {
            var str = " " + DbConversionHelper.GetAsString(column.DataType, options.UseNvar);
            if (column.MaxSize != null && DbConversionHelper.HasSize(column.DataType))
            {
                str += $"({column.MaxSize})";
            }
            else if (column.NumberPrecision != null)
            {
                str += $"({column.NumberPrecision.Scale},{column.NumberPrecision.Precision})";
            }

            str += $" {(!column.IsNullable ? "NOT " : "")}NULL";

            if (column.DefaultValue != null && column.DefaultValue.Expression != DbExpressionType.PrimaryKeySequence)
            {
                var defaultValue = string.Empty;
                if (column.DefaultValue.Expression == DbExpressionType.Constant)
                {
                    var dataType = DbConversionHelper.MapToDbDataType(column.DefaultValue.Value.GetType());
                    if (dataType != DbDataType.Unknown)
                    {
                        defaultValue = DbConversionHelper.GetValueAsString(dataType, column.DefaultValue.Value, options.UseNvar);
                    }
                }
                else
                {
                    if (column.DefaultValue.Value != null)
                    {
                        defaultValue = column.DefaultValue.Value as string;
                    }
                    else
                    {
                        defaultValue = DbConversionHelper.GetExpressionAsString(column.DefaultValue.Expression);
                    }
                }
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    str += $" DEFAULT({defaultValue})";
                }
            }
            return str;
        }
    }
}