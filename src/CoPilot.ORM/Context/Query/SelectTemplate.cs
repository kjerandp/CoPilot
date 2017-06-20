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

            var entry = new SelectTemplateEntry(setName, contextColumn);

            if (alias != null || member != null)
            {
                entry.MappedName = PathHelper.Combine(joinAlias, alias ?? member.Name);
            }

            if (!_sets.ContainsKey(setName))
            {
                _sets.Add(setName, new Set(baseNode));    
            }

            var set = _sets[setName];

            var index = set.Entries.FindIndex(r => r.Id == entry.Id);
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
            foreach (var set in _sets)
            {
                if(set.Value.JoinNode == null) continue;
                var node = set.Value.JoinNode;

                var originSetName = DetermineSetName(node.Origin);
                //Add primary key of base set if missing
                if (!_sets.ContainsKey(originSetName) || !_sets[originSetName].Entries.Any(r => r.SelectColumn.Column.Equals(node.GetSourceKey)))
                {
                    AddEntry(node.Origin, node.GetSourceKey);
                }
                //Add foreign key if missing
                if (!set.Value.Entries.Any(r => r.SelectColumn.Column.Equals(node.GetTargetKey)))
                {
                    AddEntry(node, node.GetTargetKey);
                }
            }
        }

        public class Set
        {
            public ITableContextNode BaseNode { get; }

            public Set(ITableContextNode baseNode)
            {
                BaseNode = baseNode;
                Entries = new List<SelectTemplateEntry>();
            }

            public List<SelectTemplateEntry> Entries { get; }

            public TableContextNode JoinNode { get; set; }
        }

        public class SelectTemplateEntry
        {
            public SelectTemplateEntry(string setName, ContextColumn column)
            {
                SetName = setName;
                SelectColumn = column;
                MappedName = LocalId;
            }
            public string SetName { get; set; }
            public string MappedName { get; set; }
            public ContextColumn SelectColumn { get; set; }

            public string Id => SetName + "." + LocalId;
            public string LocalId => string.IsNullOrEmpty(SelectColumn.ColumnAlias)
                    ? SelectColumn.Column.ColumnName
                    : SelectColumn.ColumnAlias;

            public override string ToString()
            {
                return Id;
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
            var indexedSets = new Dictionary<string, Dictionary<object, MappedRecord>>();
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

                if (!indexedSets.ContainsKey(targetSetName))
                {
                    indexedSets.Add(targetSetName, targetSet.ToDictionary(r => targetKeyFunc.Invoke(r), r => r));
                }
                var indexedParents = indexedSets[targetSetName];

                var targetCollection = joinNode.Origin.MapEntry.MemberToRelationshipMappings.SingleOrDefault(
                        r => r.Value.ForeignKeyColumn.Equals(sourceKey.Column)).Key;

                if (targetCollection != null)
                {
                    //Parallel.ForEach(sourceSet.Where(r => r.Instance != null), mappedRecord =>
                    //{
                    //    var fkValue = sourceKeyFunc.Invoke(mappedRecord);
                    //    var parent = indexedParents[fkValue].Instance;
                    //    ReflectionHelper.AddValueToMemberCollection(targetCollection, parent, mappedRecord.Instance);
                    //});
                    foreach (var mappedRecord in sourceSet.Where(r => r.Instance != null))
                    {
                        var fkValue = sourceKeyFunc.Invoke(mappedRecord);
                        var parent = indexedParents[fkValue].Instance;
                        ReflectionHelper.AddValueToMemberCollection(targetCollection, parent, mappedRecord.Instance);
                    }
                }

            }
        }

        private Func<MappedRecord, object> CreateKeyFunc(ContextColumn keyCol)
        {
            if (keyCol.MappedMember != null)
            {
                return m => keyCol.MappedMember.GetValue(m.Instance);
            }
            return m => m.UnmappedData[string.IsNullOrEmpty(keyCol.ColumnAlias) ? keyCol.Column.ColumnName : keyCol.ColumnAlias];
        }

        public ContextColumn GetColumn(string setName, string id)
        {
            var entry = _sets[setName].Entries.SingleOrDefault(r => r.MappedName.Equals(id));
            return entry?.SelectColumn;
        }

        public string[] GetSetNames()
        {
            return _sets.Keys.ToArray();
        }
    }
}
