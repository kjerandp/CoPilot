using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.PostgreSql.Writers;

namespace CoPilot.ORM.PostgreSql
{
    public class PostgreSqlSelectStatementBuilder : ISelectStatementBuilder
    {
        
        public QuerySegments Build(QueryContext queryContext)
        {
            var qs = new QuerySegments();

            qs.AddToSegment(QuerySegment.Select, queryContext.SelectColumns.Select(GetColumnAsText).ToArray());

            qs.AddToSegment(QuerySegment.BaseTable, $"{Util.SanitizeName(queryContext.BaseNode.Table.TableName)} T{queryContext.BaseNode.Index}");

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
                                $"T{r.Key.Node.Index}.{Util.SanitizeName(r.Key.Column.ColumnName)} {(r.Value == Ordering.Ascending ? "asc" : "desc")}"
                    ).ToArray());
                }

                if (queryContext.Modifiers != null)
                {
                    if (queryContext.Modifiers.Distinct)
                    {
                        qs.AddToSegment(QuerySegment.PreSelect, "DISTINCT");
                    }
                    var limit = "";
                    if (queryContext.Modifiers.Take.HasValue && queryContext.Modifiers.Take.Value > 0)
                    {
                        limit += $"LIMIT {queryContext.Modifiers.Take.Value}";
                    }
                    if (queryContext.Modifiers.Skip.HasValue && queryContext.Modifiers.Skip.Value > 0)
                    {
                        limit += $" OFFSET {queryContext.Modifiers.Skip.Value}";
                    }

                   
                    if (!string.IsNullOrEmpty(limit))
                    {
                        qs.AddToSegment(QuerySegment.PostOrdering, limit);
                    }
                }
            }
            
            return qs;
        }
        private static string GetColumnAsText(ContextColumn col)
        {
            var colName = $"{Util.SanitizeName(col.Column.ColumnName)}";

            var str = $"T{col.Node.Index}.{colName}";
            if (!string.IsNullOrEmpty(col.ColumnAlias))
            {
                str += $" as \"{Util.SanitizeName(col.ColumnAlias)}\"";
            }
            return str;
        }

        private static string GetFromItemText(TableJoinDescription join)
        {
            return $"{(join.JoinType == TableJoinType.InnerJoin ? "INNER" : "LEFT")} JOIN {Util.SanitizeName(join.TargetKey.Table.TableName)} T{join.TargetTableIndex} ON T{join.TargetTableIndex}.{Util.SanitizeName(join.TargetKey.ColumnName)}=T{join.SourceTableIndex}.{Util.SanitizeName(join.SourceKey.ColumnName)}";
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
                var str = $"T{cmo.ColumnReference.Node.Index}.{Util.SanitizeName(cmo.ColumnReference.Column.ColumnName)}";

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