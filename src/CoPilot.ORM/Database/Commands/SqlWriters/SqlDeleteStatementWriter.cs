using System.Collections.Generic;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public class SqlDeleteStatementWriter : IDeleteStatementWriter
    {
        public SqlStatement GetStatement(OperationContext ctx, ScriptOptions options = null)
        {
            options = options ?? ScriptOptions.Default();

            var statement = new SqlStatement();
            var qualifications = new List<string>();

            foreach (var col in ctx.Columns.Keys)
            {
                var param = ctx.Columns[col];
                var part = "{value}";
                
                var value = ctx.Args[param.Name];

                string valueString;
                if (options.Parameterize)
                {
                    valueString = part.Replace("{value}", param.Name);
                    statement.Parameters.Add(param);
                    statement.AddArgument(param.Name, value);
                }
                else
                {
                    valueString = part.Replace("{value}", DbConversionHelper.GetValueAsString(col.DataType, value, options.UseNvar));
                }
                   
                qualifications.Add($"[{col.ColumnName}] = {valueString}"); 
            }
            statement.Script.Add($"delete from {ctx.Node.Table} where {string.Join(" AND ", qualifications)}");
            
            return statement;
        }

    }
}