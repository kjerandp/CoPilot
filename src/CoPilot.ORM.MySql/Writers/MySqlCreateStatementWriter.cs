using System;
using System.Collections.Generic;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;
using System.Linq;
using CoPilot.ORM.Exceptions;

namespace CoPilot.ORM.MySql.Writers
{
    public class MySqlCreateStatementWriter : ICreateStatementWriter
    {
        private readonly MySqlProvider _provider;

        public MySqlCreateStatementWriter(MySqlProvider provider)
        {
            _provider = provider;
        }

        public SqlStatement GetStatement(DbTable table, CreateOptions options)
        {

            var pk = new List<string>();
             
            var stm = new SqlStatement();
            stm.Script.Add($"CREATE TABLE IF NOT EXISTS {table.GetAsString()} (");

            var createColumns = new ScriptBlock();
            var constraints = new ScriptBlock();

            foreach (var dbColumn in table.Columns)
            {
                var extendedInfo = "";
                
                if (dbColumn.IsPrimaryKey)
                {
                    extendedInfo = " " + GetPrimaryKeyString(dbColumn);
                    pk.Add($"{dbColumn.ColumnName.QuoteIfNeeded()}");
                   
                }
                if (dbColumn.IsForeignKey)
                {
                    constraints.Add(GetForeignKeyString(dbColumn));
                }
                createColumns.Add($"{(createColumns.ItemCount > 0 ? "," : "")}{dbColumn.ColumnName.QuoteIfNeeded()}{GetDataTypeString(dbColumn)}{extendedInfo}");
            }
           
            var uniqueColumns = table.Columns.Where(r => r.Unique);

            foreach (var uniqueColumn in uniqueColumns)
            {
                constraints.Add($",CONSTRAINT UQ_{uniqueColumn.ColumnName.Replace(" ","_")} UNIQUE({uniqueColumn.ColumnName.QuoteIfNeeded()})");
            }

            stm.Script.Add(createColumns);
            if (pk.Any())
            {
                stm.Script.Add($"\t,PRIMARY KEY ({string.Join(", ", pk)})");
            }
            if (constraints.ItemCount > 0)
            {
                stm.Script.Append(constraints);
            }
            stm.Script.Add(string.IsNullOrEmpty(_provider.Collation) ? ");" : $")\nCOLLATE {_provider.Collation};");


            return stm;
        }

        private static string GetPrimaryKeyString(DbColumn column)
        {
            var str = string.Empty;
            if (column.DefaultValue?.Expression == DbExpressionType.PrimaryKeySequence && !column.Table.HasCompositeKey)
            {
                str += "AUTO_INCREMENT ";
                
            }
            
            return str;
        }

        private static string GetForeignKeyString(DbColumn column)
        {

            var str = $"\t,FOREIGN KEY ({column.ColumnName.QuoteIfNeeded()}) REFERENCES {column.ForeignkeyRelationship.PrimaryKeyColumn.Table.GetAsString()}({column.ForeignkeyRelationship.PrimaryKeyColumn.ColumnName.QuoteIfNeeded()})";
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