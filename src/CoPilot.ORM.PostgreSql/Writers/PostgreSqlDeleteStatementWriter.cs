using System.Collections.Generic;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters;

namespace CoPilot.ORM.PostgreSql.Writers
{
    public class PostgreSqlDeleteStatementWriter : IDeleteStatementWriter
    {
        private readonly PostgreSqlProvider _provider;

        public PostgreSqlDeleteStatementWriter(PostgreSqlProvider provider)
        {
            _provider = provider;
        }
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
                    valueString = part.Replace("{value}", _provider.GetValueAsString(col.DataType, value));
                }
                   
                qualifications.Add($"{Util.SanitizeName(col.ColumnName)} = {valueString}"); 
            }
            statement.Script.Add($"delete from {Util.SanitizeName(ctx.Node.Table.TableName)} where {string.Join(" AND ", qualifications)};");
            
            return statement;
        }

    }
}