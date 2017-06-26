using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.PostgreSql.Writers
{
    public class PostgreSqlCreateStatementWriter : ICreateStatementWriter
    {
        private readonly PostgreSqlProvider _provider;

        public PostgreSqlCreateStatementWriter(PostgreSqlProvider provider)
        {
            _provider = provider;
        }

        public SqlStatement GetStatement(DbTable table, CreateOptions options)
        {             
            var stm = new SqlStatement();
            stm.Script.Add($"CREATE TABLE IF NOT EXISTS {Util.SanitizeName(table.TableName)} (");

            var createColumns = new ScriptBlock();
            var constraints = new ScriptBlock();

            foreach (var dbColumn in table.Columns)
            {
                var extendedInfo = "";

                if (dbColumn.IsPrimaryKey)
                {
                    extendedInfo = " " + GetPrimaryKeyString(dbColumn);
                  
                }
                else
                {
                    extendedInfo = " " + GetDataTypeString(dbColumn);
                }
                if (dbColumn.IsForeignKey)
                {
                    constraints.Add(GetForeignKeyString(dbColumn));
                }
                createColumns.Add($"{(createColumns.ItemCount > 0 ? "," : "")}{Util.SanitizeName(dbColumn.ColumnName)}{extendedInfo}");
            }
           
            var uniqueColumns = table.Columns.Where(r => r.Unique);

            foreach (var uniqueColumn in uniqueColumns)
            {
                constraints.Add($",CONSTRAINT UQ_{uniqueColumn.ColumnName.Replace(" ","_")} UNIQUE({Util.SanitizeName(uniqueColumn.ColumnName)})");
            }

            stm.Script.Add(createColumns);
            
            if (constraints.ItemCount > 0)
            {
                stm.Script.Append(constraints);
            }
            stm.Script.Add(");");


            return stm;
        }

        private static string GetPrimaryKeyString(DbColumn column)
        {
            var str = string.Empty;
            if (column.DefaultValue?.Expression == DbExpressionType.PrimaryKeySequence && !column.Table.HasCompositeKey)
            {
                str += column.DataType == DbDataType.Int64 ? "BIGSERIAL " : "SERIAL ";
                
            }
            
            return str + "PRIMARY KEY";
        }

        private static string GetForeignKeyString(DbColumn column)
        {

            var str = $"\t,FOREIGN KEY ({Util.SanitizeName(column.ColumnName)}) REFERENCES {Util.SanitizeName(column.ForeignkeyRelationship.PrimaryKeyColumn.Table.TableName)} ({Util.SanitizeName(column.ForeignkeyRelationship.PrimaryKeyColumn.ColumnName)})";
            return str;
        }

        private string GetDataTypeString(DbColumn column)
        {
            var dataTypeText = _provider.GetDataTypeAsString(column.DataType, column.MaxSize);
            if (column.NumberPrecision != null && dataTypeText.IndexOf("<precision>", StringComparison.Ordinal) > 0)
            {
                dataTypeText = dataTypeText.Replace("<precision>", $"({column.NumberPrecision.Scale},{column.NumberPrecision.Precision})");
            }
            var str = " " + dataTypeText;
            
            str += $" {(!column.IsNullable ? "NOT " : "")}NULL";
            if (!string.IsNullOrEmpty(_provider.Collation))
            {
                str += $" COLLATE {Util.SanitizeName(_provider.Collation)}";
            }
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
                    throw new CoPilotUnsupportedException();
                case DbExpressionType.CurrentDate:
                    throw new CoPilotUnsupportedException();
                case DbExpressionType.CurrentDateTime:
                    throw new CoPilotUnsupportedException();
                case DbExpressionType.Guid:
                    throw new CoPilotUnsupportedException();
                case DbExpressionType.SequencialGuid:
                    throw new CoPilotUnsupportedException();
                case DbExpressionType.PrimaryKeySequence:
                    throw new CoPilotUnsupportedException();
                default: return null;
            }
        }
    }
}