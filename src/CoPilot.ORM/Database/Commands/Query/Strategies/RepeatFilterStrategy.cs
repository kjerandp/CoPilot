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
    public class RepeatFilterStrategy : IQueryExecutionStrategy, IQueryScriptCreator
    {
        private readonly IQueryBuilder _builder;
        private readonly ISelectStatementWriter _writer;

        public RepeatFilterStrategy(IQueryBuilder builder, ISelectStatementWriter writer)
        {
            _builder = builder;
            _writer = writer;
        }
        public IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader)
        {
            string[] names;

            var stm = CreateStatement(node, filter, out names);

            var response = reader.Query(stm, names);
            Console.WriteLine($"Took: {response.ElapsedMs}ms");
            return ContextMapper.MapAndMerge(node, response.RecordSets);
        }

        public SqlStatement CreateStatement(ITableContextNode node, FilterGraph filter, out string[] names)
        {
            var ctx = node.Context;
            if (ctx.Predicates != null) throw new NotSupportedException("This query strategy cannot be used with predicates!");
            var q = ctx.GetQueryContext(node, filter);
            var stm = q.GetStatement(_builder, _writer);
            var namesList = new List<string> { node.Path };

            AddContextNodeQueries(node, stm, filter, namesList);
            names = namesList.ToArray();
            return stm;
        }

        private void AddContextNodeQueries(ITableContextNode parentNode, SqlStatement stm, FilterGraph filter, List<string> names)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;
                if (node.IsInverted)
                {
                    var q = node.GetQueryContext(filter);
                    stm.Script.Add();
                    stm.Script.Append(_writer.GetStatement(_builder.Build(q)));
                    names.Add(node.Path);

                }

                AddContextNodeQueries(node, stm, filter, names);
            }
        }
    }
}