using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping.Mappers;

namespace CoPilot.ORM.Database.Commands.Query.Strategies
{
    internal class MultipleQueriesStrategy : IQueryExecutionStrategy
    {
        private readonly IQueryBuilder _builder;
        private readonly ISelectStatementWriter _writer;

        public MultipleQueriesStrategy(IQueryBuilder builder, ISelectStatementWriter writer)
        {
            _builder = builder;
            _writer = writer;
        }
        public IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader)
        {
            var ctx = node.Context;
            var recordsets = new List<DbRecordSet>();

            var q = ctx.GetQueryContext(node, filter);

            var stm = q.GetStatement(_builder, _writer);
            var baseRecords = ExecuteStatement(node, stm, reader);
            recordsets.Add(baseRecords);

            ExecuteNodeQueries(node, baseRecords, filter, reader, recordsets);

            return ContextMapper.MapAndMerge(ctx, recordsets);
        }

        private void ExecuteNodeQueries(ITableContextNode parentNode, DbRecordSet parentSet, FilterGraph filter, DbReader reader, ICollection<DbRecordSet> rs)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;
                var set = parentSet;
                if (node.IsInverted)
                {
                    var keyCol = node.Origin.Table.GetSingularKey();
                    var childFilter = filter;
                    if (keyCol != null)
                    {
                        var fieldName = PathHelper.MaskPath(parentNode.Path + "." + keyCol.ColumnName, parentSet.Name);
                        var fieldIndex = parentSet.GetIndex(fieldName);
                        if (fieldIndex >= 0)
                        {
                            var keyValues = parentSet.Vector(fieldIndex);
                            childFilter = FilterGraph.CreateChildFilter(node, keyValues);
                        }
                    }
                    var q = node.Context.GetQueryContext(node, childFilter);
                    var stm = q.GetStatement(_builder, _writer);
                    var data = ExecuteStatement(node, stm, reader);
                    rs.Add(data);
                    set = data;
                }
                ExecuteNodeQueries(node, set, filter, reader, rs);
            }
        }

        private static DbRecordSet ExecuteStatement(ITableContextNode node, DbRequest statement, DbReader reader)
        {
            var queryResult = reader.Query(statement, node.Path);
            var data = queryResult.RecordSets.Single();

            return data;
        }

        
    }
}