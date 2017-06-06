using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Exceptions;
using System.Linq;

namespace CoPilot.ORM.Providers.SqlServer
{
    public class SqlQueryBuilder : IQueryBuilder
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
            qs.AddToSegment(QuerySegment.Select, queryContext.SelectColumns.Select(r => r.ToString()).ToArray());

            qs.AddToSegment(QuerySegment.BaseTable, $"{SanitizeTableName(queryContext.BaseNode.Table.TableName)} T{queryContext.BaseNode.Index}");

            qs.AddToSegment(QuerySegment.Joins, queryContext.JoinedNodes.Select(GetFromItemText).ToArray());
            
            if (queryContext.Filter?.Root != null)
            {
                qs.AddToSegment(QuerySegment.Filter, queryContext.Filter.Root.ToString());
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

        
        private static string GetFromItemText(TableJoinDescription join)
        {
            return $"{(join.JoinType == TableJoinType.InnerJoin ? "INNER" : "LEFT")} JOIN {SanitizeTableName(join.TargetKey.Table.TableName)} T{join.TargetTableIndex} ON T{join.TargetTableIndex}.{join.TargetKey.ColumnName}=T{join.SourceTableIndex}.{join.SourceKey.ColumnName}";
        }

        private static string SanitizeTableName(string tableName)
        {
            return tableName.Contains(" ") ? "[" + tableName + "]" : tableName;
        }

    }
}