using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Mapping.Mappers;

namespace CoPilot.ORM.Database.Commands.Query.Strategies
{
    public class TempTableWhereStrategy : IQueryExecutionStrategy
    {
        public IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader)
        {
            var ctx = node.Context;
            
            var stm = GetStatement(ctx, filter);
            var names = new List<string> {node.Path};

            AddContextNodeQueries(node, stm, names);

            var response = reader.Query(stm, names.ToArray());
            Console.WriteLine($"Took: {response.ElapsedMs}ms");
            return ContextMapper.MapAndMerge(node, response.RecordSets);
        }

        private SqlStatement GetStatement(ITableContextNode node, FilterGraph filter)
        {
            var ctx = node.Context;
            var writer = ctx.Model.ResourceLocator.Get<ISelectStatementWriter>();
            var builder = ctx.Model.ResourceLocator.Get<IQueryBuilder>();
            var q = ctx.GetQueryContext(node, filter);
            var segments = builder.Build(q);
            var tempName = node.Path.Replace(".", "_");

            if (node.Nodes.Any(r => r.Value.IsInverted))
            {    
                segments.AddToSegment(QuerySegment.PostSelect, $"INTO #{tempName}");
            }

            var stm = writer.GetStatement(segments);

            if (segments.Exist(QuerySegment.PostSelect))
            {
                stm.Script.Add($"\nSELECT * FROM #{tempName}\n");
            }
            return stm;
        }

        private void AddContextNodeQueries(ITableContextNode parentNode, SqlStatement stm, List<string> names)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;
                if (node.IsInverted)
                {
                    var filter = FilterGraph.CreateChildFilterUsingTempTable(node, "#" + parentNode.Path.Replace(".", "_"));
                    var cStm = GetStatement(node, filter);
                    
                    stm.Script.Append(cStm.Script);
                    names.Add(node.Path);
                    
                }

                AddContextNodeQueries(node, stm, names);
            }
        }
    }
}