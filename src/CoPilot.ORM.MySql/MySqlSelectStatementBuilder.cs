
using System;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;
using System.Linq;

namespace CoPilot.ORM.MySql
{
    public class MySqlSelectStatementBuilder : ISelectStatementBuilder
    {
        
        public QuerySegments Build(QueryContext queryContext)
        {
            var qs = new QuerySegments();

            qs.AddToSegment(QuerySegment.Select, queryContext.SelectColumns.Select(GetColumnAsText).ToArray());

            qs.AddToSegment(QuerySegment.BaseTable, $"{queryContext.BaseNode.Table.GetAsString()} T{queryContext.BaseNode.Index}");

            qs.AddToSegment(QuerySegment.Joins, queryContext.JoinedNodes.Select(GetFromItemText).ToArray());
            
            if (queryContext.Filter?.Root != null)
            {
                qs.AddToSegment(QuerySegment.Filter, GetFilterOperandAsText(queryContext.Filter.Root));
            }

            if (queryContext.BaseNode.Level == 0)
            {
                if (queryContext.OrderByClause != null && queryContext.OrderByClause.Any())
                {
                    qs.AddToSegment(QuerySegment.Ordering, queryContext.OrderByClause.Select(r =>
                                $"T{r.Key.Node.Index}.{r.Key.Column.ColumnName.QuoteIfNeeded()} {(r.Value == Ordering.Ascending ? "asc" : "desc")}"
                    ).ToArray());
                }

                if (queryContext.Modifiers != null)
                {
                    if (queryContext.Modifiers.Distinct)
                    {
                        qs.AddToSegment(QuerySegment.PreSelect, "DISTINCT");
                    }
                    var limit = new Tuple<int, int>(queryContext.Modifiers.Skip ?? 0, queryContext.Modifiers.Take ?? 0);

                    if (limit.Item1 > 0 && limit.Item2 == 0)
                    {
                        throw new CoPilotUnsupportedException("Can't skip records without specifying how many records to take.");
                    }

                    if (limit.Item2 > 0)
                    {
                        qs.AddToSegment(QuerySegment.PostOrdering, $"LIMIT {(limit.Item1 > 0 ? limit.Item1+",":"")}{limit.Item2}");
                    }
                }
            }
            
            return qs;
        }
        private static string GetColumnAsText(ContextColumn col)
        {
            var colName = col.Column.ColumnName.QuoteIfNeeded();

            var str = $"T{col.Node.Index}.{colName}";
            if (!string.IsNullOrEmpty(col.ColumnAlias))
            {
                str += $" as '{col.ColumnAlias}'";
            }
            return str;
        }

        private static string GetFromItemText(TableJoinDescription join)
        {
            return $"{(join.JoinType == TableJoinType.InnerJoin ? "INNER" : "LEFT")} JOIN {join.TargetKey.Table.GetAsString()} T{join.TargetTableIndex} ON T{join.TargetTableIndex}.{join.TargetKey.ColumnName.QuoteIfNeeded()}=T{join.SourceTableIndex}.{join.SourceKey.ColumnName.QuoteIfNeeded()}";
        }

        private static string GetFilterOperandAsText(IExpressionOperand operand)
        {
            var bin = operand as BinaryOperand;
            if (bin != null)
            {
                var str = $"{GetFilterOperandAsText(bin.Left)} {Defaults.GetOperatorAsText(bin.Operator)} {GetFilterOperandAsText(bin.Right)}";
                if (bin.Enclose)
                {
                    str = $"({str})";
                }
                return str;
            }

            var cmo = operand as MemberExpressionOperand;
            if (cmo != null)
            {
                var str = $"T{cmo.ColumnReference.Node.Index}.{cmo.ColumnReference.Column.ColumnName.QuoteIfNeeded()}";

                if (!string.IsNullOrEmpty(cmo.Custom))
                {
                    return cmo.Custom.Replace("{column}", str);
                }
                if (!string.IsNullOrEmpty(cmo.WrapWith))
                {
                    str = $"{cmo.WrapWith}({str})";
                }
                return str;
            }

            return operand.ToString();
        }

     

    }
}