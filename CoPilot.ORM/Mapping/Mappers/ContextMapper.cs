using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Mapping.Mappers
{
    /// <summary>
    /// Maps to POCO object mapped with the DbMapper 
    /// </summary>
    public static class ContextMapper
    {
        /// <summary>
        /// Create a mapping delegate using the ContextMapper
        /// </summary>
        /// <param name="node">The table context node to map</param>
        /// <returns>Mapping delegate</returns>
        public static ObjectMapper Create(ITableContextNode node)
        {
            ObjectMapper mapper = dataset =>
            {
                var mapping = BuildMapping(node, dataset.FieldNames.Select((f, i) => new IndexedFieldName(i, f)).ToArray());
                var records = ExecuteMappingNode(mapping, dataset);
                return records;
            };
            return mapper;
        }

        private static ContextMappingNode BuildMapping(ITableContextNode node, IndexedFieldName[] fieldNames)
        {
            var cmNode = new ContextMappingNode
            {
                Node = node,
                ColumnToMemberDictionary = new Dictionary<FieldColumEntry, ClassMemberInfo>(),
                SubNodes = new Dictionary<ClassMemberInfo, ContextMappingNode>()
            };

            var relatedEntities = new Dictionary<string, List<IndexedFieldName>>();

            foreach (var entry in fieldNames)
            {
                var firstDot = entry.FieldName.IndexOf(".", StringComparison.Ordinal);
                if (firstDot > 0)
                {
                    var rKey = entry.FieldName.Substring(0, firstDot);
                    var rPropKey = entry.FieldName.Substring(firstDot + 1, entry.FieldName.Length - (firstDot + 1));
                    if (!relatedEntities.ContainsKey(rKey))
                    {
                        relatedEntities.Add(rKey, new List<IndexedFieldName>());
                    }
                    relatedEntities[rKey].Add(new IndexedFieldName(entry.Index, rPropKey));
                }
                else
                {
                    var col = node.Table.GetColumnByName(entry.FieldName);
                    if (col == null)
                    {
                        continue;
                    }
                    var member = node.MapEntry.GetMappedMember(col);
                    cmNode.ColumnToMemberDictionary.Add(new FieldColumEntry(entry.Index, col), member);
                }
            }

            foreach (var rKey in relatedEntities.Keys)
            {
                if (!node.Nodes.ContainsKey(rKey))
                {
                    continue;
                }
                var rNode = node.Nodes[rKey];
                var members = node.MapEntry.EntityType.GetMember(rKey);
                var rRecord = relatedEntities[rKey];

                if (members.Length == 1)
                {
                    var member = ClassMemberInfo.Create(members.Single());
                    cmNode.SubNodes.Add(member, BuildMapping(rNode, rRecord.ToArray()));
                }
                else
                {
                    throw new ArgumentException($"Unable to match a single member with name '{rKey}' on object '{node.MapEntry.EntityType.Name}'");
                }
            }

            return cmNode;
        }

        private static MappedRecord[] ExecuteMappingNode(ContextMappingNode mappingNode, DbRecordSet dataset)
        {
            var records = new MappedRecord[dataset.Records.Length];
            Parallel.ForEach(dataset.Records, (r, n, i) =>
            {
                records[i] = MapSingleInstance(mappingNode, r);
            });
            //for (var r = 0; r < dataset.Records.Length; r++)
            //{
            //    records[r] = MapSingleInstance(mappingNode, dataset.Records[r]);
            //}
            return records;
        }

        private static MappedRecord MapSingleInstance(ContextMappingNode mappingNode, object[] values)
        {
            var rec = new MappedRecord()
            {
                UnmappedData = new Dictionary<string, object>(),
                Instance = null
            };

            if (values == null || values.All(r => r is DBNull))
            {
                return rec;
            }

            rec.Instance = ReflectionHelper.CreateInstance(mappingNode.Node.MapEntry.EntityType);

            foreach (var col in mappingNode.ColumnToMemberDictionary.Keys)
            {
                var value = values[col.FieldIndex];
                
                var member = mappingNode.ColumnToMemberDictionary[col];
                if (member == null)
                {
                    rec.UnmappedData.Add(col.Column.ColumnName, value);
                }
                else
                {
                    mappingNode.Node.MapEntry.SetValueOnMappedMember(member, rec.Instance, value);
                }
            }

            if (mappingNode.SubNodes.Any())
            {
                foreach (var member in mappingNode.SubNodes.Keys)
                {
                    var subNode = mappingNode.SubNodes[member];
                    var subRec = MapSingleInstance(subNode, values);
                    mappingNode.Node.MapEntry.SetValueOnMappedMember(member, rec.Instance, subRec.Instance);
                }
            }

            return rec;
        }
        
        private struct ContextMappingNode
        {
            public ITableContextNode Node { get; set; }
            public Dictionary<FieldColumEntry, ClassMemberInfo> ColumnToMemberDictionary { get; set; }
            public Dictionary<ClassMemberInfo, ContextMappingNode> SubNodes { get; set; }
        }

        private struct IndexedFieldName
        {
            public IndexedFieldName(int index, string fieldName)
            {
                Index = index;
                FieldName = fieldName;
            }
            public int Index { get; }
            public string FieldName { get; }
        }

        private struct FieldColumEntry
        {
            public FieldColumEntry(int index, DbColumn col)
            {
                FieldIndex = index;
                Column = col;
            }
            public int FieldIndex { get; }
            public DbColumn Column { get; }
        }

        public static IEnumerable<T> MapAndMerge<T>(TableContext<T> baseNode, IEnumerable<DbRecordSet> recordSets)
            where T : class
        {
            return MapAndMerge(baseNode as ITableContextNode, recordSets).OfType<T>();
        }

        public static IEnumerable<object> MapAndMerge(ITableContextNode baseNode, IEnumerable<DbRecordSet> recordSets)
        {
            //var w = Stopwatch.StartNew();
            if (recordSets == null) return new object[0];

            var dbRecordSets = recordSets.ToArray();
            for (var i=0; i<dbRecordSets.Length;i++)
            {
                var n = PathHelper.SplitFirstInPathString(dbRecordSets[i].Name);
                if (!n.Item1.Equals("Base", StringComparison.Ordinal))
                {
                    dbRecordSets[i].Name = "Base" + (string.IsNullOrEmpty(n.Item2)?"":"."+n.Item2);
                }
            }
            var sets = dbRecordSets.ToDictionary(k => k.Name, v => v);

            if(sets.Count == 0) return new object[0];
            
            var mapper = Create(baseNode);
            var baseSet = sets[baseNode.Path];
            var mapped = mapper.Invoke(baseSet);
            
            ProcessNode(baseNode, "", baseSet, mapped, sets);
            
            //Console.WriteLine("Mapping took: " +w.ElapsedMilliseconds);
            return mapped.Select(r => r.Instance);
           
        }

        private static void ProcessNode(ITableContextNode node, string prefix, DbRecordSet parentSet, MappedRecord[] parentRecords, IDictionary<string, DbRecordSet> sets)
        {
            Parallel.ForEach(node.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship), rel =>
            {
                var relNode = rel.Value;
                if (relNode.IsInverted)
                {
                    var pkName = prefix + relNode.GetSourceKey.ColumnName;
                    var pkIndex = parentSet.GetIndex(pkName);

                    var indexed = new Dictionary<object, MappedRecord>();
                    for (var i = 0; i < parentRecords.Length; i++)
                    {
                        var keyValue = parentSet.Records[i][pkIndex];
                        indexed.Add(keyValue, parentRecords[i]);
                    }

                    var fkName = relNode.GetTargetKey.ColumnName;

                    if (sets.ContainsKey(relNode.Path))
                    {
                        var childSet = sets[relNode.Path];
                        var fkIndex = childSet.GetIndex(fkName);
                        var mapper = Create(relNode);
                        var mapped = mapper.Invoke(childSet).ToArray();
                        //merge
                        for (var i = 0; i < mapped.Length; i++)
                        {
                            var keyValue = childSet.Records[i][fkIndex];
                            if (indexed.ContainsKey(keyValue))
                            {
                                var instance = indexed[keyValue].Instance;
                                var target = PathHelper.GetReferenceFromPath(instance, prefix + rel.Key);
                                ReflectionHelper.AddValueToMemberCollection(target.Item2, target.Item1, mapped[i].Instance, false);
                            }

                        }

                        ProcessNode(relNode, "", childSet, mapped, sets);
                    }

                }
                else
                {
                    var newPrefix = prefix;
                    if (string.IsNullOrEmpty(newPrefix))
                    {
                        newPrefix = rel.Key + ".";
                    }
                    else
                    {
                        newPrefix = newPrefix + rel.Key + ".";
                    }
                    ProcessNode(relNode, newPrefix, parentSet, parentRecords, sets);
                }
            });
            //foreach (var rel in node.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            //{
                
            //}
        }

       
    }

    
}
