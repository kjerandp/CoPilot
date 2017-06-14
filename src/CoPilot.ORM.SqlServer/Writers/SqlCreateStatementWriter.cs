using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.SqlServer.Writers
{
    public class SqlCreateStatementWriter : ICreateStatementWriter
    {
        private readonly SqlServerProvider _provider;

        public SqlCreateStatementWriter(SqlServerProvider provider)
        {
            _provider = provider;
        }

        public SqlStatement GetStatement(DbTable table, CreateOptions options)
        {
            List<string> compositeKeys = null;

            var hasCompositeKey = table.HasCompositeKey;
            if(hasCompositeKey) compositeKeys = new List<string>();

            var stm = new SqlStatement();
            stm.Script.Add($"CREATE TABLE [{table.Schema}].[{table.TableName}] (");
            var createColumns = new ScriptBlock();
            foreach (var dbColumn in table.Columns)
            {
                var extendedInfo = "";
                
                if (dbColumn.IsPrimaryKey)
                {
                    if (hasCompositeKey)
                    {
                        compositeKeys.Add("["+dbColumn.ColumnName+"]");
                    }
                    else
                    {
                        extendedInfo = " " + GetPrimaryKeyString(dbColumn, options);
                    }
                }
                else if (dbColumn.IsForeignKey)
                {
                    extendedInfo = " " + GetForeignKeyString(dbColumn);
                }
                createColumns.Add($"{(createColumns.ItemCount > 0 ? "," : "")}{dbColumn.ColumnName}{GetDataTypeString(dbColumn)}{extendedInfo}");
            }

            if (compositeKeys != null)
            {
                createColumns.Add($"CONSTRAINT PK_{table.TableName.Replace(" ", "_")} PRIMARY KEY NONCLUSTERED ({string.Join(", ", compositeKeys)})");
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
            if (column.DefaultValue?.Expression == DbExpressionType.PrimaryKeySequence)
            {
                str += $"IDENTITY({options.KeySequenceStartAt},{options.KeySequenceIncrementBy}) ";
                
            }
            //else if (column.DefaultValue?.Value != null)
            //{
            //    str += column.DefaultValue.Value as string;
            //    str += " ";
            //}

            return str + "PRIMARY KEY";
        }

        private static string GetForeignKeyString(DbColumn column)
        {

            var str = $"REFERENCES {column.ForeignkeyRelationship.PrimaryKeyColumn.Table.TableName}({column.ForeignkeyRelationship.PrimaryKeyColumn.ColumnName})";
            return str;
        }

        private string GetDataTypeString(DbColumn column)
        {
            var str = " " + _provider.GetDataTypeAsString(column.DataType, column.MaxSize);
            
            if (column.NumberPrecision != null)
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
                        defaultValue = _provider.GetValueAsString(dataType, column.DefaultValue.Value);
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
                        defaultValue = GetDbExpressionAsString(column.DefaultValue.Expression);
                    }
                }
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    str += $" DEFAULT({defaultValue})";
                }
            }
            return str;
        }

        private static string GetDbExpressionAsString(DbExpressionType expression)
        {
            switch (expression)
            {
                case DbExpressionType.Timestamp:
                    return "GETDATE()";
                case DbExpressionType.CurrentDate:
                    return "GETDATE()";
                case DbExpressionType.CurrentDateTime:
                    return "GETDATE()";
                case DbExpressionType.Guid:
                    return "NEWID()";
                case DbExpressionType.SequencialGuid:
                    return "NEWSEQUENTIALID()";
                case DbExpressionType.PrimaryKeySequence:
                    return "IDENTITY(1,1)";
                default: return null;
            }
        }
    }
}