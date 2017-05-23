using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping.Mappers;

namespace CoPilot.ORM.Database.Commands.ContextQueryStrategies
{
    public class ContextQueryTempTableStrategy : IContextQueryStrategy
    {
        public IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader)
        {
            var ctx = node.Context;
            
            var q = ctx.GetQueryContext(node, filter);
            var writer = ctx.Model.ResourceLocator.Get<ISelectStatementWriter>();

            var stm = writer.GetSelectIntoStatement(q);
            var names = new List<string> {node.Path};

            AddContextNodeQueries(node, stm, names, writer);

            var response = reader.Query(stm, names.ToArray());

            return ContextMapper.MapAndMerge(node, response.RecordSets);
        }

        private void AddContextNodeQueries(ITableContextNode parentNode, SqlStatement stm, List<string> names, ISelectStatementWriter writer)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;
                if (node.IsInverted)
                {
                    var filter = FilterGraph.CreateChildFilterUsingTempTable(node, "#" + parentNode.Path.Replace(".", "_"));

                    var q = parentNode.Context.GetQueryContext(node, filter);
                    var cStm = node.Nodes.Any(r => r.Value.IsInverted) ? 
                        writer.GetSelectIntoStatement(q) : 
                        writer.GetStatement(q);
                    
                    stm.Script.Append(cStm.Script);
                    names.Add(node.Path);
                    
                }

                AddContextNodeQueries(node, stm, names, writer);
                

            }
        }

        
        /*
        internal static IEnumerable<object> ExecuteContextQuery(ITableContextNode node, FilterGraph filter,
            SqlCommand command, ISelectStatementWriter writer, Type mapToType = null)
        {
            var q = node.Context.GetQueryContext(node, filter);
            
            var stm = writer.GetStatement(q);

            var baseRecords = ExecuteStatement(node, stm, command);
            recordsets.Add(baseRecords);

            if (mapToType != null && mapToType != node.MapEntry.EntityType)
            {
                var mapper = (mapToType == typeof(object) || mapToType == typeof(IDictionary<string, object>))
                    ? DynamicMapper.Create(false)
                    : BasicMapper.Create(mapToType);

                return mapper.Invoke(baseRecords).Select(r => r.Instance);
            }

            ExecuteNodeQueries(node, baseRecords, writer, command, recordsets);

            

        }

        private static void ExecuteNodeQueries(ITableContextNode parentNode, DbRecordSet parentSet, 
            ISelectStatementWriter writer, SqlCommand command, List<DbRecordSet> rs)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;

                if (node.IsInverted)
                {
                    var childFilter  = CreateChildFilterFromParentKeys(parentNode, parentSet, node);
                    
                    var q = node.Context.GetQueryContext(node, childFilter);

                    var stm = writer.GetStatement(q);
                    var data = ExecuteStatement(node, stm, command);
                    rs.Add(data);
                    ExecuteNodeQueries(node, data, writer, command, rs);
                }
                else
                {
                    ExecuteNodeQueries(node, parentSet, writer, command, rs);
                }

            }
        }

        

        private static FilterGraph CreateChildFilterFromParentKeys(ITableContextNode parentNode, DbRecordSet parentSet,
            TableContextNode node)
        {
            var keyCol = node.Origin.Table.GetSingularKey();
            var fieldName = PathHelper.MaskPath(parentNode.Path + "." + keyCol.ColumnName, parentSet.Name);
            return FilterGraph.CreateChildFilterUsingTempTable(node, fieldName, "#" + parentNode.Path.Replace(".", "_"));

        }

        private static DbRecordSet ExecuteStatement(ITableContextNode node, DbRequest statement, SqlCommand command)
        {
            var queryResult = CommandExecutor.ExecuteQuery(command, statement, node.Path);
            var data = queryResult.RecordSets.Single();

            return data;
        }

        */
    }
}