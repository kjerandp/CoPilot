using System;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public class SqlInsertStatementWriter : IInsertStatementWriter
    {
        public SqlStatement GetStatement(OperationContext ctx, ScriptOptions options)
        {
            var statement = new SqlStatement();

            var colBlock = new ScriptBlock();
            var valBlock = new ScriptBlock();
 
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
                            throw new ArgumentException($"No value specified for the non-nullable column '{col.ColumnName}'.");
                        }
                    }
                }
                if (value == null) continue;

                string valueString;
                if (options.Parameterize)
                {
                    valueString = part.Replace("{value}", param.Name);
                    statement.Parameters.Add(param);
                    statement.Args.Add(param.Name, value);
                }
                else
                {
                    valueString = part.Replace("{value}", DbConversionHelper.GetValueAsString(col.DataType, value, options.UseNvar));
                }
                colBlock.Add($"{(colBlock.ItemCount > 0 ? "," : "")}{col.ColumnName}");
                valBlock.Add($"{(valBlock.ItemCount > 0 ? "," : "")}{valueString}");
            }

            statement.Script.Add($"insert into {ctx.Node.Table} (");
            statement.Script.Add(colBlock);
            statement.Script.Add(") values (");
            statement.Script.Add(valBlock);
            statement.Script.Add(")");
            if (!options.EnableIdentityInsert && options.SelectScopeIdentity)
            {
                statement.Script.Add("SELECT SCOPE_IDENTITY()");
            }
            return statement;
        }

        private string GetLookupSubQuery(DbRelationship lookupRel)
        {
            var q = $"(SELECT {lookupRel.PrimaryKeyColumn.ColumnName} FROM {lookupRel.PrimaryKeyColumn.Table} WHERE {lookupRel.LookupColumn.ColumnName} = {{value}})";

            return q;
        }
    }
}