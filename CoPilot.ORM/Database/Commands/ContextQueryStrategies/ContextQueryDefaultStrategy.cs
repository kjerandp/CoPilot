using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Mapping.Mappers;

namespace CoPilot.ORM.Database.Commands.ContextQueryStrategies
{
    internal class ContextQueryDefaultStrategy : IContextQueryStrategy
    {
        public IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader)
        {
            var ctx = node.Context;
            var recordsets = new List<DbRecordSet>();

            var q = ctx.GetQueryContext(node, filter);
            var writer = ctx.Model.ResourceLocator.Get<ISelectStatementWriter>();
            var builder = ctx.Model.ResourceLocator.Get<IQueryBuilder>();
            var stm = writer.GetStatement(builder.Build(q));
            var baseRecords = ExecuteStatement(node, stm, reader);
            recordsets.Add(baseRecords);

            ExecuteNodeQueries(node, baseRecords, filter, builder, writer, reader, recordsets);

            return ContextMapper.MapAndMerge(ctx, recordsets);
        }

        private static void ExecuteNodeQueries(ITableContextNode parentNode, DbRecordSet parentSet, FilterGraph filter, IQueryBuilder builder, ISelectStatementWriter writer, DbReader reader, ICollection<DbRecordSet> rs)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;
                var set = parentSet;
                if (node.IsInverted)
                {
                    //var keyCol = node.Origin.Table.GetSingularKey();
                    //var childFilter = filter;
                    //if (keyCol != null && parentSet.Records.Length <= 10)
                    //{
                    //    var fieldName = PathHelper.MaskPath(parentNode.Path + "." + keyCol.ColumnName, parentSet.Name);
                    //    var fieldIndex = parentSet.GetIndex(fieldName);
                    //    if (fieldIndex >= 0)
                    //    {
                    //        var keyValues = parentSet.Vector(fieldIndex);
                    //        childFilter = FilterGraph.CreateChildFilter(node, keyValues);
                    //    }
                    //}
                    var q = node.Context.GetQueryContext(node, filter);
                    var stm = writer.GetStatement(builder.Build(q));
                    var data = ExecuteStatement(node, stm, reader);
                    rs.Add(data);
                    set = data;
                }
                ExecuteNodeQueries(node, set, filter, builder, writer, reader, rs);
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