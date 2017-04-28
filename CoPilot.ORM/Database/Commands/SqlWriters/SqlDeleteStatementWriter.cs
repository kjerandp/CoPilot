using System.Linq;
using CoPilot.ORM.Common;
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
            var key = ctx.Columns.Keys.Single();
            var parameter = ctx.Columns[key];
            var valuePart = parameter.Name;
            var value = ctx.Args[parameter.Name];

            if (!options.Parameterize)
            {
                valuePart = DbConversionHelper.GetValueAsString(key.DataType, value, options.UseNvar);
            }
            else
            {
                statement.Parameters.Add(parameter);
                statement.Args.Add(parameter.Name, value);
            }

            statement.Script.Add($"delete from {key.Table} where {key.ColumnName} = {valuePart}");
            
            return statement;
        }

    }
}