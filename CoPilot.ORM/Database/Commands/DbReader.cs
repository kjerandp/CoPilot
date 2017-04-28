using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping.Mappers;

namespace CoPilot.ORM.Database.Commands
{
    internal class DbReader
    {
        internal static IEnumerable<object> ExecuteContextQuery(ITableContextNode node, FilterGraph filter,
            SqlCommand command, ISelectStatementWriter writer, Type mapToType = null)
        {
            var recordsets = new List<DbRecordSet>();
            var q = node.Context.GetQueryContext(node, filter);
            var stm = writer.GetStatement(q);

            var baseRecords = ExecuteStatement(node, stm, command);
            recordsets.Add(baseRecords);

            if (mapToType != null && mapToType != node.MapEntry.EntityType)
            {
                var mapper = (mapToType == typeof(object) || mapToType == typeof(IDictionary<string, object>)) ?
                                    DynamicMapper.Create(false) :
                                    BasicMapper.Create(mapToType);

                return mapper.Invoke(baseRecords).Select(r => r.Instance);
            }

            ExecuteNodeQueries(node, baseRecords, filter, writer, command, recordsets);
            
            return ContextMapper.MapAndMerge(node, recordsets);

        }

        private static void ExecuteNodeQueries(ITableContextNode parentNode, DbRecordSet parentSet, FilterGraph filter, ISelectStatementWriter writer, SqlCommand command, List<DbRecordSet> rs)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;

                if (node.IsInverted)
                {
                    var keyCol = node.Origin.Table.GetKey();
                    var childFilter = filter;
                    if (keyCol != null && parentSet.Records.Length <= 10)
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
                    var stm = writer.GetStatement(q);
                    var data = ExecuteStatement(node, stm, command);
                    rs.Add(data);
                    ExecuteNodeQueries(node, data, filter, writer, command, rs);
                }
                else
                {
                    ExecuteNodeQueries(node, parentSet, filter, writer, command, rs);
                }
                
            }
        }

        private static DbRecordSet ExecuteStatement(ITableContextNode node, DbRequest statement, SqlCommand command)
        {
            var queryResult = CommandExecutor.ExecuteQuery(command, statement, node.Path);
            var data = queryResult.RecordSets.Single();

            return data;
        }

       
    }
}