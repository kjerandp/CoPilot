using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Mapping.Mappers;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Database.Commands.Query.Strategies
{
    public class TempTableWhereStrategy : IQueryExecutionStrategy
    {
        private readonly IQueryBuilder _builder;
        private readonly ISelectStatementWriter _writer;

        public TempTableWhereStrategy(IQueryBuilder builder, ISelectStatementWriter writer)
        {
            _builder = builder;
            _writer = writer;
        }
        public IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader)
        {
            var ctx = node.Context;
            var q = ctx.GetQueryContext(node, filter);
            var stm = new SqlStatement(GetStatement(q));
            if (q.Filter != null)
            {
                stm.Parameters.AddRange(q.Filter.Parameters);
                stm.Args = q.Filter.Arguments;
            }
            var names = new List<string> { node.Path };

            AddContextNodeQueries(node, stm, names);

            var response = reader.Query(stm, names.ToArray());
            Console.WriteLine($"Took: {response.ElapsedMs}ms");
            return ContextMapper.MapAndMerge(node, response.RecordSets);
           
        }

        private ScriptBlock GetStatement(QueryContext q)
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
                    var filter = FilterGraph.CreateChildFilterUsingTempTable(node, "#" + parentNode.Path.Replace(".", "_"));
                    var cStm = GetStatement(node.GetQueryContext(filter));
                    
                    stm.Script.Append(cStm);
                    names.Add(node.Path);
                    
                }

                AddContextNodeQueries(node, stm, names);
            }
        }
    }
}