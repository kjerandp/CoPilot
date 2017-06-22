using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Context.Query
{
    public class SelectTemplate
    {
        public Delegate ShapeFunc { get; set; }
        
        private readonly Dictionary<string, Set> _sets;

        public SelectTemplate()
        {
            _sets = new Dictionary<string, Set>();
        }

        public void AddEntry(ITableContextNode node, DbColumn column, ClassMemberInfo member = null, string joinAlias = null, string alias = null)
        {
            var baseNode = DetermineBaseNode(node);
            var setName = baseNode.Path;

            joinAlias = joinAlias ?? PathHelper.MaskPath(node.Path, setName);

            if (column == null)
            {
                
            }

            var contextColumn = ContextColumn.Create(node, column, joinAlias, alias);

            var entry = new SelectTemplateEntry(contextColumn);

            if (alias != null || member != null)
            {
                entry.MappedName = PathHelper.Combine(joinAlias, alias ?? member.Name);
            }

            if (!_sets.ContainsKey(setName))
            {
                _sets.Add(setName, new Set(baseNode));    
            }

            var set = _sets[setName];

            var index = set.Entries.FindIndex(r => r.LocalId == entry.LocalId);
            if (index >= 0)
            {
                set.Entries[index] = entry;
            }
            else
            {
                set.Entries.Add(entry);
            }

            if (node.Level > 0)
            {
                var tableContextNode = node as TableContextNode;
                if (tableContextNode != null && tableContextNode.IsInverted)
                {
                    set.JoinNode = tableContextNode;
                }
            }
        }

        public void Complete()
        {
            var keys = _sets.Select(r => r.Key).ToArray();
            foreach (var key in keys)
            {
                var set = _sets[key];

                if(set.JoinNode == null) continue;
                var node = set.JoinNode;

                var originSetName = DetermineSetName(node.Origin);
                //Add primary key of base set if missing
                if (!_sets.ContainsKey(originSetName) || !_sets[originSetName].Entries.Any(r => r.SelectColumn.Column.Equals(node.GetSourceKey)))
                {
                    AddEntry(node.Origin, node.GetSourceKey);
                }
                //Add foreign key if missing
                if (!set.Entries.Any(r => r.SelectColumn.Column.Equals(node.GetTargetKey)))
                {
                    AddEntry(node, node.GetTargetKey);
                }
            }
        }
        
        public static string DetermineSetName(ITableContextNode node)
        {
            return DetermineBaseNode(node).Path;
        }

        public static ITableContextNode DetermineBaseNode(ITableContextNode node)
        {
            var current = node as TableContextNode;

            if (current == null) return node;

            while (current != null)
            {
                if (current.IsInverted) break;
                if (current.Origin is TableContext) return current.Origin;
                current = current.Origin as TableContextNode;
            }
            if (current != null) return current;

            throw new CoPilotRuntimeException($"Unable to find base node from node '{node.Path}'");
        }

        public void AddNode(ITableContextNode node)
        {
            var mappedColumns = node.MapEntry.GetMappedColumns();
            foreach (var map in mappedColumns.Where(r => !r.Key.ExcludeFromSelect && r.Value != null))
            {
                AddEntry(node, map.Key, map.Value);
            }      
        }
        
        public static SelectTemplate BuildFrom(TableContext tableContext)
        {
            var template = new SelectTemplate();
            template.AddNode(tableContext);
            tableContext.IncludedNodes.ForEach(template.AddNode);
            template.Complete();
            return template;
        }

        public ContextColumn[] GetColumnsInSet(string setName)
        {
            if(!_sets.ContainsKey(setName)) return new ContextColumn[0];
            return _sets[setName].Entries.Select(r => r.SelectColumn).ToArray();
        }

        public Dictionary<string, ContextColumn> GetDictionaryFromSet(string setName)
        {
            return _sets[setName].Entries.ToDictionary(r => r.LocalId, r => r.SelectColumn);
        }

        public ITableContextNode GetBaseNode(string setName)
        {
            return _sets[setName].BaseNode;
        }

        public void Merge(IReadOnlyDictionary<string, MappedRecord[]> data)
        {
            var indexedSets = new Dictionary<string, Dictionary<object, MappedRecord[]>>();
            foreach (var set in _sets.Where(r => r.Value.JoinNode != null))
            {
                var joinNode = set.Value.JoinNode;
                var targetSetName = DetermineSetName(joinNode.Origin);
                var targetSet = data[targetSetName];
                var sourceSet = data[set.Key];

                var targetKey = _sets[targetSetName].Entries.Single(r => r.SelectColumn.Column.Equals(joinNode.GetSourceKey)).SelectColumn;
                var sourceKey = set.Value.Entries.Single(r => r.SelectColumn.Column.Equals(joinNode.GetTargetKey)).SelectColumn;

                var targetKeyFunc = CreateKeyFunc(targetKey);
                var sourceKeyFunc = CreateKeyFunc(sourceKey);

                var indexedSetName = PathHelper.Combine(targetSetName, targetKey.Name);
                if (!indexedSets.ContainsKey(indexedSetName))
                {
                    indexedSets.Add(indexedSetName, CreateIndexedSet(targetSet, targetKeyFunc));
                }
                var indexedParents = indexedSets[indexedSetName];

                var targetCollection = joinNode.Origin.MapEntry.MemberToRelationshipMappings.SingleOrDefault(
                        r => r.Value.ForeignKeyColumn.Equals(sourceKey.Column)).Key;

                if (targetCollection != null)
                {
                    Parallel.ForEach(sourceSet.Where(r => r.Instance != null), mappedRecord =>
                    {
                        var fkValue = sourceKeyFunc.Invoke(mappedRecord);
                        foreach (var record in indexedParents[fkValue])
                        {
                            var targetInstance = GetInstanceFromPath(
                                record.Instance,
                                PathHelper.RemoveLastElementFromPathString(targetKey.Name),
                                targetCollection.Name
                            );
                            ReflectionHelper.AddValueToMemberCollection(targetCollection, targetInstance, mappedRecord.Instance);
                        }
                    });
                    //foreach (var mappedRecord in sourceSet.Where(r => r.Instance != null))
                    //{
                    //    var fkValue = sourceKeyFunc.Invoke(mappedRecord);
                    //    foreach (var record in indexedParents[fkValue])
                    //    {
                    //        var targetInstance = GetInstanceFromPath(
                    //            record.Instance, 
                    //            PathHelper.RemoveLastElementFromPathString(targetKey.Name), 
                    //            targetCollection.Name
                    //        );
                    //        ReflectionHelper.AddValueToMemberCollection(targetCollection, targetInstance, mappedRecord.Instance);
                    //    }
                        
                    //}
                }

            }
        }

        public ContextColumn GetColumn(string setName, string id)
        {
            return _sets[setName].Entries.Where(r => r.MappedName.Equals(id)).Select(r => r.SelectColumn).SingleOrDefault();
        }

        public string[] GetSetNames()
        {
            return _sets.Keys.ToArray();
        }

        private object GetInstanceFromPath(object baseInstance, string basePath, string memberName)
        {
            object instance;
            if (string.IsNullOrEmpty(basePath))
            {
                instance = baseInstance;
            }
            else
            {
                var objectReference = PathHelper.GetReferenceFromPath(baseInstance, PathHelper.Combine(basePath, memberName));
                instance = objectReference.Item1;
            }

            return instance;
        }

        private Dictionary<object, MappedRecord[]> CreateIndexedSet(MappedRecord[] targetSet, Func<MappedRecord, object> targetKeyFunc)
        {
            var indexedRecords = new Tuple<object, MappedRecord>[targetSet.Length];

            Parallel.ForEach(targetSet, (mappedRecord, s, i) =>
            {
                indexedRecords[i] = new Tuple<object, MappedRecord>(targetKeyFunc.Invoke(mappedRecord),mappedRecord);
            });
            var indexedSet = indexedRecords.GroupBy(r => r.Item1, r => r.Item2, (k, v) => new { Key = k, Records = v.ToArray() })
                .ToDictionary(r => r.Key, r => r.Records);

            return indexedSet;
        }

        private Func<MappedRecord, object> CreateKeyFunc(ContextColumn keyCol)
        {
            if (keyCol.MappedMember != null)
            {
                return m =>
                {
                    var path = PathHelper.RemoveLastElementFromPathString(keyCol.Name);
                    var instance = GetInstanceFromPath(m.Instance, path, keyCol.MappedMember.Name);
                    return keyCol.MappedMember.GetValue(instance);
                };
            }
            return m => m.UnmappedData[string.IsNullOrEmpty(keyCol.ColumnAlias) ? keyCol.Column.ColumnName : keyCol.ColumnAlias];
        }


        private class Set
        {
            public ITableContextNode BaseNode { get; }

            public Set(ITableContextNode baseNode)
            {
                BaseNode = baseNode;
                Entries = new List<SelectTemplateEntry>();
                JoinNode = null;
            }

            public List<SelectTemplateEntry> Entries { get; }

            public TableContextNode JoinNode { get; set; }
        }

        private struct SelectTemplateEntry
        {
            public SelectTemplateEntry(ContextColumn column)
            {
                SelectColumn = column;
                LocalId = string.IsNullOrEmpty(SelectColumn.ColumnAlias)
                    ? SelectColumn.Column.ColumnName
                    : SelectColumn.ColumnAlias;

                MappedName = LocalId;
            }

            public string MappedName { get; set; }
            public ContextColumn SelectColumn { get; }
            public string LocalId { get; } 
            public override string ToString()
            {
                return MappedName;
            }
        }
    }
}
