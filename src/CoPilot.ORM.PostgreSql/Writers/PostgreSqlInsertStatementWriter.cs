using System.Linq;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.PostgreSql.Writers
{
    public class PostgreSqlInsertStatementWriter : IInsertStatementWriter
    {
        private readonly PostgreSqlProvider _provider;

        public PostgreSqlInsertStatementWriter(PostgreSqlProvider provider)
        {
            _provider = provider;
        }

        public SqlStatement GetStatement(OperationContext ctx, ScriptOptions options)
        {
            var statement = new SqlStatement();
            var colBlock = new ScriptBlock();
            var valBlock = new ScriptBlock();
            var identityInsertUsed = !ctx.Columns.Keys.Any(r => r.IsPrimaryKey);

            foreach (var col in ctx.Columns.Keys)
            {
                var param = ctx.Columns[col];
                var part = "{value}";
                object value = null;
                if (ctx.Args.ContainsKey(param.Name))
                {
                    value = ctx.Args[param.Name];
                    if (col.IsPrimaryKey)
                    {
                        if (col.DefaultValue?.Expression == DbExpressionType.PrimaryKeySequence && (!options.EnableIdentityInsert || value == null || value.Equals(ReflectionHelper.GetDefaultValue(value.GetType()))))
                        {
                            identityInsertUsed = true;
                            continue;
                        }
                    }

                    if (col.ForeignkeyRelationship != null && col.ForeignkeyRelationship.IsLookupRelationship)
                    {
                        part = GetLookupSubQuery(col.ForeignkeyRelationship);
                    }
                }
                else
                {
                    if (!col.IsNullable && col.DefaultValue != null)
                    {
                        value = col.DefaultValue.CreateDefaultValue();
                        if (value == null && !col.IsPrimaryKey || options.EnableIdentityInsert) { 
                            throw new CoPilotDataException($"No value specified for the non-nullable column '{col.ColumnName}'.");
                        }
                    }
                }
                if (value == null) continue;

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
                colBlock.Add($"{(colBlock.ItemCount > 0 ? "," : "")}{col.ColumnName.QuoteIfNeeded()}");
                valBlock.Add($"{(valBlock.ItemCount > 0 ? "," : "")}{valueString}");
            }

            statement.Script.Add($"insert into {ctx.Node.Table.GetAsString()} (");
            statement.Script.Add(colBlock);
            statement.Script.Add(") values (");
            statement.Script.Add(valBlock);

            if (identityInsertUsed && !options.EnableIdentityInsert && options.SelectScopeIdentity)
            {
                var pkCol = ctx.Node.Table.GetSingularKey();
                statement.Script.Add($") RETURNING {pkCol.ColumnName.QuoteIfNeeded()};");
            }
            else
            {
                statement.Script.Add(");");
            }
            return statement;
        }

        private string GetLookupSubQuery(DbRelationship lookupRel)
        {
            var q = $"(SELECT {lookupRel.PrimaryKeyColumn.ColumnName.QuoteIfNeeded()} FROM {lookupRel.PrimaryKeyColumn.Table.GetAsString()} WHERE {lookupRel.LookupColumn.ColumnName.QuoteIfNeeded()} = {{value}})";

            return q;
        }
    }
}