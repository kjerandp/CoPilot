using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;

namespace CoPilot.ORM.SqlServer
{
    public class SqlSelectStatementBuilder : ISelectStatementBuilder
    {
        public QuerySegments Build(QueryContext queryContext)
        {
            var qs = new QuerySegments();

            if (queryContext.BaseNode.Level == 0)
            {
                if (queryContext.Modifiers != null)
                {
                    if (queryContext.Modifiers.Distinct)
                    {
                        qs.AddToSegment(QuerySegment.PreSelect, "DISTINCT");
                    }
                    if (queryContext.OrderByClause == null || !queryContext.OrderByClause.Any() || !queryContext.Modifiers.Skip.HasValue)
                    {
                        if (queryContext.Modifiers.Skip.HasValue)
                            throw new CoPilotUnsupportedException("Need to specify an orderby-clause to use SKIP/TAKE");
                        if (queryContext.Modifiers.Take.HasValue)
                            qs.AddToSegment(QuerySegment.PreSelect, $"TOP {queryContext.Modifiers.Take.Value}");
                    }
                    else
                    {
                        if (queryContext.Modifiers.Skip != null)
                        {
                            qs.AddToSegment(QuerySegment.PostOrdering,
                                $"OFFSET {queryContext.Modifiers.Skip.Value} ROWS");

                            if (queryContext.Modifiers?.Take != null)
                            {
                                qs.AddToSegment(QuerySegment.PostOrdering,
                                    $"FETCH NEXT {queryContext.Modifiers.Take.Value} ROWS ONLY");
                            }
                        }
                        
                    }
                }

                if (queryContext.OrderByClause != null && queryContext.OrderByClause.Any())
                {
                    qs.AddToSegment(QuerySegment.Ordering, queryContext.OrderByClause.Select(r =>
                                $"T{r.Key.Node.Index}.{SanitizeName(r.Key.Column.ColumnName)} {(r.Value == Ordering.Ascending ? "asc" : "desc")}"
                    ).ToArray());
                }
            }
            qs.AddToSegment(QuerySegment.Select, queryContext.SelectColumns.Select(GetColumnAsText).ToArray());

            qs.AddToSegment(QuerySegment.BaseTable, $"{SanitizeName(queryContext.BaseNode.Table.TableName)} T{queryContext.BaseNode.Index}");

            qs.AddToSegment(QuerySegment.Joins, queryContext.JoinedNodes.Select(GetFromItemText).ToArray());
            
            if (queryContext.Filter?.Root != null)
            {
                qs.AddToSegment(QuerySegment.Filter, GetFilterOperandAsText(queryContext.Filter.Root));
            }

            return qs;
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
                var str = $"T{cmo.ColumnReference.Node.Index}.{SanitizeName(cmo.ColumnReference.Column.ColumnName)}";

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

        

        private static string GetColumnAsText(ContextColumn col)
        {
            var colName = SanitizeName(col.Column.ColumnName);
            
            var str = $"T{col.Node.Index}.{colName}";
            if (!string.IsNullOrEmpty(col.ColumnAlias))
            {
                str += $" as [{col.ColumnAlias}]";
            }
            return str;
        }
        
        private static string GetFromItemText(TableJoinDescription join)
        {
            return $"{(join.JoinType == TableJoinType.InnerJoin ? "INNER" : "LEFT")} JOIN {SanitizeName(join.TargetKey.Table.TableName)} T{join.TargetTableIndex} ON T{join.TargetTableIndex}.{SanitizeName(join.TargetKey.ColumnName)}=T{join.SourceTableIndex}.{SanitizeName(join.SourceKey.ColumnName)}";
        }

        private static string SanitizeName(string name)
        {
            return name.Contains(" ") ? "[" + name + "]" : name;
        }

    }
}