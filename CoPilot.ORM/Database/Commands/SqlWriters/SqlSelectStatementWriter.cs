using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public class SqlSelectStatementWriter : ISelectStatementWriter
    {
        private readonly IFilterExpressionWriter _filterWriter;
        public SqlSelectStatementWriter(IFilterExpressionWriter filterExpressionWriter)
        {
            _filterWriter = filterExpressionWriter;
        }   
        public SqlStatement GetStatement(QueryContext queryContext)
        {
            var statement = new SqlStatement();
            var parts = new Dictionary<string, string> { { "SELECT", "" }, { "FROM", "" }, {"WHERE", ""} };
            
            if (queryContext.BaseNode.Level == 0 && queryContext.Predicates != null)
            {
                if (queryContext.Predicates.Distinct)
                {
                    parts["SELECT"] += " DISTINCT";
                }
                if (queryContext.Predicates.Top.HasValue)
                {
                    parts["SELECT"] += " TOP "+queryContext.Predicates.Top.Value;
                }
            }

            parts["SELECT"] += "\n\t" + string.Join("\n\t,", queryContext.SelectColumns.Select(r => r.ToString()));

            parts["FROM"] = $"\n\t{queryContext.BaseNode.Table.TableName} T{queryContext.BaseNode.Index}";

            foreach (var item in queryContext.JoinedNodes)
            {
                parts["FROM"] += GetFromItemText(item);
            }
            
            var sql = $"SELECT{parts["SELECT"]}\nFROM{parts["FROM"]}";

            if (queryContext.Filter?.Root != null)
            {
                parts["WHERE"] = _filterWriter.GetExpression(queryContext.Filter, statement.Parameters, statement.Args);
            }

            if (!string.IsNullOrEmpty(parts["WHERE"]))
            {
                sql += "\nWHERE\n\t"+parts["WHERE"];
            }

            if (queryContext.BaseNode.Level==0 && queryContext.OrderByClause != null && queryContext.OrderByClause.Any())
            {
                sql+="\nORDER BY"+string.Join("\n\t,",queryContext.OrderByClause.Select(r => $"\n\tT{r.Key.Node.Index}.{r.Key.Column.ColumnName} {(r.Value == Ordering.Ascending ? "asc" : "desc")}"));

                if (queryContext.Predicates?.Skip != null)
                {
                    sql += $"\nOFFSET {queryContext.Predicates.Skip.Value} ROWS";
                    if (queryContext.Predicates?.Take != null)
                    {
                        sql += $"\nFETCH NEXT {queryContext.Predicates.Take.Value} ROWS ONLY";
                    }
                }  
            }
            statement.Script.Add(sql);
            return statement;
        }

        private static string GetFromItemText(TableJoinDescription join)
        {
            return $"\n\t{(join.JoinType == TableJoinType.InnerJoin ? "INNER" : "LEFT")} JOIN {join.TargetKey.Table.TableName} T{join.TargetTableIndex} ON T{join.TargetTableIndex}.{join.TargetKey.ColumnName}=T{join.SourceTableIndex}.{join.SourceKey.ColumnName}";
        }

    }

    
}
