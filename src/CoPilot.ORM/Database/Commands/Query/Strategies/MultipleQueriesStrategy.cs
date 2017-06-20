using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping.Mappers;

namespace CoPilot.ORM.Database.Commands.Query.Strategies
{
    public class MultipleQueriesStrategy : IQueryExecutionStrategy
    {
        private readonly ISelectStatementBuilder _builder;
        private readonly ISelectStatementWriter _writer;

        public MultipleQueriesStrategy(ISelectStatementBuilder builder, ISelectStatementWriter writer)
        {
            _builder = builder;
            _writer = writer;
        }
        public IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader)
        {
            var recordsets = new List<DbRecordSet>();

            var q = QueryContext.Create(node, filter);

            var stm = q.GetStatement(_builder, _writer);
            var baseRecords = ExecuteStatement(node, stm, reader);
            recordsets.Add(baseRecords);
            
            ExecuteNodeQueries(node, baseRecords, filter, reader, recordsets);

            return ContextMapper.MapAndMerge(node.Context.SelectTemplate, recordsets.ToArray());
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
                            childFilter = CreateChildFilter(node, keyValues);
                        }
                    }
                    var q = QueryContext.Create(node, childFilter);
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

        private static FilterGraph CreateChildFilter(TableContextNode node, object[] keys)
        {
            var filter = new FilterGraph();
            var left = new MemberExpressionOperand(ContextColumn.Create(node, node.GetTargetKey));
            var right = new ValueListOperand("@id", keys);
            filter.Root = new BinaryOperand(left, right, SqlOperator.In);

            return filter;
        }
    }
}