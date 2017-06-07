using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Providers.MySql.Writers
{
    public class MySqlUpdateStatementWriter : IUpdateStatementWriter
    {
        private readonly MySqlServerProvider _provider;

        public MySqlUpdateStatementWriter(MySqlServerProvider provider)
        {
            _provider = provider;
        }

        public SqlStatement GetStatement(OperationContext ctx, ScriptOptions options = null)
        {
            options = options ?? ScriptOptions.Default();

            var statement = new SqlStatement();

            var colBlock = new ScriptBlock();

            var qualifications = new List<string>();

            foreach (var col in ctx.Columns.Keys)
            {
                var param = ctx.Columns[col];
                var part = "{value}";

                if (ctx.Args.ContainsKey(param.Name))
                {
                    var value = ctx.Args[param.Name];
                    if (col.ForeignkeyRelationship != null && col.ForeignkeyRelationship.IsLookupRelationship)
                    {
                        part = GetLookupSubQuery(col.ForeignkeyRelationship);
                    }

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
                    if (col.IsPrimaryKey)
                    {
                        qualifications.Add($"[{col.ColumnName}] = {valueString}");
                    }
                    else
                    {
                        colBlock.Add($"{(colBlock.ItemCount > 0 ? "," : "")}{col.ColumnName} = {valueString}");
                    }
                }
                else
                {
                     throw new CoPilotDataException($"No argument specified for the parameter '{param.Name}'.");  
                }
            }
            if (!qualifications.Any())
                throw new CoPilotUnsupportedException("Key column not found among the columns provided by the operation context!");

            statement.Script.Add($"UPDATE {ctx.Node.Table} SET");
            statement.Script.Add(colBlock);
            statement.Script.Add("WHERE");
            statement.Script.Add(new ScriptBlock(string.Join(" AND ", qualifications)));

            return statement;
        }

        private string GetLookupSubQuery(DbRelationship lookupRel)
        {
            var q = $"(SELECT {lookupRel.PrimaryKeyColumn.ColumnName} FROM {lookupRel.PrimaryKeyColumn.Table} WHERE {lookupRel.LookupColumn.ColumnName} = {{value}})";

            return q;
        }
    }
}