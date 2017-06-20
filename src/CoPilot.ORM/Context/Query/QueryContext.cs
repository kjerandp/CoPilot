using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering;

namespace CoPilot.ORM.Context.Query
{
    public struct QueryContext
    {
        public ContextColumn[] SelectColumns { get; internal set; }
        public Dictionary<ContextColumn, Ordering> OrderByClause { get; internal set; }
        public ITableContextNode BaseNode { get; internal set; }
        public TableJoinDescription[] JoinedNodes { get; internal set; }
        public FilterGraph Filter { get; set; }
        public SelectModifiers Modifiers { get; internal set; }

        public static QueryContext Create(ITableContextNode node, FilterGraph filter = null)
        {
            var ctx = node.Context;
            var baseNode = node;
            
            if (ctx.SelectTemplate == null)
            {
                ctx.SelectTemplate = SelectTemplate.BuildFrom(ctx);
            }
            var template = ctx.SelectTemplate;

            var selectColumns = template.GetColumnsInSet(SelectTemplate.DetermineSetName(node));

            if (selectColumns == null || !selectColumns.Any())
            {
                throw new CoPilotUnsupportedException("No columns in select list!");
            }

            var referencedNodes = selectColumns.Select(r => r.Node).Select(r => new FromListItem(r, false)).ToList();
            if (filter?.Root != null)
            {
                var filterNodes = filter.MemberExpressions.Select(r => r.ColumnReference.Node);
                foreach (var filterNode in filterNodes)
                {
                    var tcn = filterNode as TableContextNode;
                    if (tcn != null && tcn.IsInverted && tcn != baseNode) throw new CoPilotUnsupportedException("Invalid filter expression");
                    referencedNodes.Add(new FromListItem(filterNode, tcn == null || !tcn.IsInverted));
                }
            }
            if (node == ctx && ctx.Ordering != null && ctx.Ordering.Any())
            {
                referencedNodes.AddRange(ctx.Ordering.Select(r => new FromListItem(r.Key.Node, false)));
            }

            var fromList = new List<FromListItem>(referencedNodes.Distinct().OrderBy(r => r.ForceInnerJoin ? 1 : r.Node.Order).ThenBy(r => r.Node.Level));

            var currentIndex = fromList.Count - 1;

            while (currentIndex > 0)
            {
                var currentNode = fromList[currentIndex].Node as TableContextNode;
                if (currentNode == null) break;
                var depNodeExist = fromList.Exists(r => r.Node.Index == currentNode.Origin.Index);
                if (!depNodeExist)
                    fromList.Insert(currentIndex, new FromListItem(currentNode.Origin, currentNode.JoinType == TableJoinType.InnerJoin));

                currentIndex--;
            }

            if (fromList[0].Node != baseNode && fromList[0].Node.Level < baseNode.Level)
            {
                baseNode = fromList[0].Node;
            }

            fromList.RemoveAll(r => r.Node == baseNode);

            return new QueryContext
            {
                SelectColumns = selectColumns.ToArray(),
                OrderByClause = ctx.Ordering,
                Modifiers = ctx.SelectModifiers,
                BaseNode = baseNode,
                JoinedNodes = fromList.Select(r => new TableJoinDescription(r)).ToArray(),
                Filter = filter
            };
        }
        
        public SqlStatement GetStatement(ISelectStatementBuilder builder, ISelectStatementWriter writer)
        {
            var qs = builder.Build(this);
            var stm = new SqlStatement(writer.GetStatement(qs));
            if (Filter != null)
            {
                stm.Parameters = Filter.Parameters.ToList();
                stm.Args = Filter.Arguments;
            }
            
            return stm;
        }
    }
}