using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.SqlServer.Writers
{
    public class TempTableWhereWriter : ISingleStatementQueryWriter
    {
        private readonly ISelectStatementBuilder _builder;
        private readonly ISelectStatementWriter _writer;

        public TempTableWhereWriter(ISelectStatementBuilder builder, ISelectStatementWriter writer)
        {
            _builder = builder;
            _writer = writer;
        }
      
        public SqlStatement CreateStatement(ITableContextNode node, FilterGraph filter, out string[] names)
        {
            var q = QueryContext.Create(node, filter);
            var stm = new SqlStatement(GetScript(q));
            if (q.Filter != null)
            {
                stm.Parameters.AddRange(q.Filter.Parameters);
                stm.Args = q.Filter.Arguments;
            }
            var namesList = new List<string> { node.Path };

            AddContextNodeQueries(node, stm, namesList);

            names = namesList.ToArray();
            return stm;
        }

        private ScriptBlock GetScript(QueryContext q)
        {
            var segments = _builder.Build(q);
            var tempName = q.BaseNode.Path.Replace(".", "_");

            if (q.BaseNode.Nodes.Any(r => r.Value.IsInverted))
            {    
                segments.AddToSegment(QuerySegment.PostSelect, $"INTO #{tempName}");
            }

            var script = _writer.GetStatement(segments);

            if (segments.Exist(QuerySegment.PostSelect))
            {
                script.Add($"\nSELECT * FROM #{tempName}\n");
            }
            return script;
        }

        private void AddContextNodeQueries(ITableContextNode parentNode, SqlStatement stm, List<string> names)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;
                if (node.IsInverted)
                {
                    var filter = CreateChildFilterUsingTempTable(node, "#" + parentNode.Path.Replace(".", "_"));
                    var cStm = GetScript(QueryContext.Create(node, filter));
                    
                    stm.Script.Append(cStm);
                    names.Add(node.Path);
                    
                }

                AddContextNodeQueries(node, stm, names);
            }
        }

        private static FilterGraph CreateChildFilterUsingTempTable(TableContextNode node, string tempTableName)
        {
            var filter = new FilterGraph();
            var left = new MemberExpressionOperand(ContextColumn.Create(node, node.GetTargetKey));
            var right = new CustomOperand($"(Select {node.GetSourceKey.ColumnName.QuoteIfNeeded()} from {tempTableName})");
            filter.Root = new BinaryOperand(left, right, SqlOperator.In);

            return filter;

        }
    }
}