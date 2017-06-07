using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;

namespace CoPilot.ORM.Providers.MySql
{
    public class MySqlQueryBuilder : IQueryBuilder
    {
        
        public QuerySegments Build(QueryContext queryContext)
        {
            var qs = new QuerySegments();

            if (queryContext.BaseNode.Level == 0 && queryContext.Predicates != null)
            {
                if ((queryContext.OrderByClause == null || !queryContext.OrderByClause.Any()) && (queryContext.Predicates.Skip.HasValue || queryContext.Predicates.Take.HasValue))
                {
                    throw new CoPilotUnsupportedException("Need to specify an orderby-clause to use SKIP/TAKE");
                }
                if (queryContext.Predicates.Distinct)
                {
                    qs.AddToSegment(QuerySegment.PreSelect, "DISTINCT");
                }
                if (queryContext.Predicates.Top.HasValue)
                {
                    qs.AddToSegment(QuerySegment.PreSelect, $"TOP {queryContext.Predicates.Top.Value}");
                }
            }
            qs.AddToSegment(QuerySegment.Select, queryContext.SelectColumns.Select(GetColumnAsText).ToArray());

            qs.AddToSegment(QuerySegment.BaseTable, $"{SanitizeName(queryContext.BaseNode.Table.TableName)} T{queryContext.BaseNode.Index}");

            qs.AddToSegment(QuerySegment.Joins, queryContext.JoinedNodes.Select(GetFromItemText).ToArray());
            
            if (queryContext.Filter?.Root != null)
            {
                qs.AddToSegment(QuerySegment.Filter, GetFilterOperandAsText(queryContext.Filter.Root));
            }

           
            if (queryContext.BaseNode.Level == 0 && queryContext.OrderByClause != null && queryContext.OrderByClause.Any())
            {
                qs.AddToSegment(QuerySegment.Ordering, queryContext.OrderByClause.Select(r =>
                            $"T{r.Key.Node.Index}.{r.Key.Column.ColumnName} {(r.Value == Ordering.Ascending ? "asc" : "desc")}"
                ).ToArray());


                if (queryContext.Predicates?.Skip != null)
                {
                    qs.AddToSegment(QuerySegment.PostOrdering, $"OFFSET {queryContext.Predicates.Skip.Value} ROWS");
                    
                    if (queryContext.Predicates?.Take != null)
                    {
                        qs.AddToSegment(QuerySegment.PostOrdering, $"FETCH NEXT {queryContext.Predicates.Take.Value} ROWS ONLY");
                    }
                }
            }
            return qs;
        }
        private static string GetColumnAsText(ContextColumn col)
        {
            var colName = SanitizeName(col.Column.ColumnName);

            var str = $"T{col.Node.Index}.{colName}";
            if (!string.IsNullOrEmpty(col.ColumnAlias))
            {
                str += $" as '{col.ColumnAlias}'";
            }
            return str;
        }

        private static string GetFromItemText(TableJoinDescription join)
        {
            return $"{(join.JoinType == TableJoinType.InnerJoin ? "INNER" : "LEFT")} JOIN {SanitizeName(join.TargetKey.Table.TableName)} T{join.TargetTableIndex} ON T{join.TargetTableIndex}.{join.TargetKey.ColumnName}=T{join.SourceTableIndex}.{join.SourceKey.ColumnName}";
        }

        private static string GetFilterOperandAsText(IExpressionOperand operand)
        {
            var bin = operand as BinaryOperand;
            if (bin != null)
            {
                var str = $"{GetFilterOperandAsText(bin.Left)} {bin.Operator} {GetFilterOperandAsText(bin.Right)}";
                if (bin.Enclose)
                {
                    str = $"({str})";
                }
                return str;
            }

            var cmo = operand as ContextMemberOperand;
            if (cmo != null)
            {
                var str = $"T{cmo.ContextColumn.Node.Index}.{cmo.ContextColumn.Column.ColumnName}";

                if (!string.IsNullOrEmpty(cmo.MemberExpressionOperand?.Custom))
                {
                    return cmo.MemberExpressionOperand.Custom.Replace("{column}", str);
                }
                if (!string.IsNullOrEmpty(cmo.MemberExpressionOperand?.WrapWith))
                {
                    str = $"{cmo.MemberExpressionOperand.WrapWith}({str})";
                }
                return str;
            }

            return operand.ToString();
        }

        private static string SanitizeName(string name)
        {
            return name.Contains(" ") ? "`" + name + "`" : name;
        }

    }
}