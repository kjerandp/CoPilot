using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Config;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;
using System.Reflection;
using CoPilot.ORM.Exceptions;

namespace CoPilot.ORM.Context
{
    public class TableContext : ITableContextNode
    {
        internal TableContext(DbModel model, TableMapEntry map)
        {
            _nodeIndex = new Dictionary<int, ITableContextNode> { { _index, this } };
            Model = model;
            SelectTemplate = new Dictionary<string, string>();

            MapEntry = map;
            Index = _index;
            Nodes = new Dictionary<string, TableContextNode>();
            CreateLookupNodesIfNotExist(this);
            
        }
        internal TableContext(DbModel model, Type baseType, params string[] include)
        {
            _nodeIndex = new Dictionary<int, ITableContextNode> { { _index, this } };
            _include = include;
            Model = model;
            SelectTemplate = new Dictionary<string, string>();

            MapEntry = Model.GetTableMap(baseType);
            if(MapEntry == null)
                throw new CoPilotConfigurationException($"'{baseType.Name}' is not mapped!");
            Index = _index;
            Nodes = new Dictionary<string, TableContextNode>();
            CreateLookupNodesIfNotExist(this);
            if (include != null)
            {
                foreach (var path in include)
                {
                    AddPath(path);
                }
            }
        }

        public ITableContextNode Origin => null;
        public Dictionary<string, TableContextNode> Nodes { get; }
        public readonly Dictionary<string, string> SelectTemplate;
        public SelectModifiers SelectModifiers { get; private set; }

        protected FilterGraph RootFilter;

        private readonly Dictionary<int, ITableContextNode> _nodeIndex;
        private int _index = 1;

        private readonly string[] _include;
        public readonly DbModel Model;
        private Dictionary<ContextColumn, Ordering> _ordering;

        
        public int Index { get; }
        public int Level => 0;
        public int Order => 0;
        public string Path => "Base";
        public TableContext Context => this;

        public ITableContextNode Previous()
        {
            return this;
        }

        public TableMapEntry MapEntry { get; }

        public DbTable Table => MapEntry?.Table;
        
        protected void AddPath(string path, bool addToSelect = true)
        {
            
            var splitPaths = path.Split('.');
            ITableContextNode currentNode = this;

            foreach (var part in splitPaths)
            {
                var member = currentNode.MapEntry.GetMemberByName(part);
                var rel = currentNode.MapEntry.GetRelationshipByMember(member);
                if (rel == null) throw new CoPilotConfigurationException($"There are no relationships that corresponds to the path '{path}' for type '{currentNode.MapEntry.EntityType.Name}'.");
                var isInverse = rel.PrimaryKeyColumn.Table == currentNode.Table;
                if (currentNode.Nodes.ContainsKey(part))
                {
                    currentNode = currentNode.Nodes[part];
                }
                else
                {
                    var memberType = member.MemberType; 
                    if (memberType.IsCollection())
                    {
                        memberType = memberType.GetCollectionType();
                    }
                    var mapEntry = Model.GetTableMap(memberType);
                    if(mapEntry == null) throw new CoPilotUnsupportedException("Can only create context node from mapped entities!");
                    var newNode = new TableContextNode(currentNode, rel, isInverse, ++_index, mapEntry);
                    _nodeIndex.Add(_index, newNode);

                    if (addToSelect)
                    {
                        CreateLookupNodesIfNotExist(newNode);
                    }
                    
                    currentNode.Nodes.Add(part, newNode);
                    currentNode = newNode;
                }
                
            }
        }
        
        internal List<ContextColumn> GetSelectColumnsFromNode(ITableContextNode node)
        {
            if(SelectTemplate.Any()) return GetSelectColumnsFromTemplate();

            var list = new List<ContextColumn>();
            var nodePath = PathHelper.RemoveFirstElementFromPathString(node.Path);
            var includes = _include.Where(r => r.StartsWith(nodePath));
            //if (!string.IsNullOrEmpty(nodePath))
            //{
            //    var parts = nodePath.Count(r => r == '.') + 1;
            //    includes = includes.Select(r => string.Join(".", r.Split('.').Skip(parts))).Where(s => s != string.Empty);
            //}

            
            AddSelectColumnsFromNode(node, list, "");

            foreach (var path in includes)
            {
                var relatedNodes = GetAllNodesInPath(PathHelper.MaskPath(path, nodePath), node);
                if (relatedNodes != null)
                {
                    foreach (var nodeInPath in relatedNodes)
                    {
                        var join = nodeInPath as TableContextNode;
                        if (join != null && join.IsInverted) break;
                        AddSelectColumnsFromNode(nodeInPath, list, PathHelper.MaskPath(nodeInPath.Path, node.Path)); //
                    }
                }
            }
            return list;
        }

        public QueryContext GetQueryContext(FilterGraph filter)
        {
            return GetQueryContext(null, filter);
        }

        public QueryContext GetQueryContext(ITableContextNode node = null, FilterGraph filter = null)
        {
            node = node ?? this;

            var baseNode = node;

            var selectColumns = GetSelectColumnsFromNode(node);

            var referencedNodes = selectColumns.Select(r => r.Node).Select(r => new FromListItem(r, false)).ToList();
            if (filter?.Root != null)
            {
                referencedNodes.AddRange(filter.MemberExpressions.Select(r => r.ColumnReference.Node).Select(r => new FromListItem(r, true)));
            }
            if (node == this && _ordering != null && _ordering.Any())
            {
                referencedNodes.AddRange(_ordering.Select(r => new FromListItem(r.Key.Node, false)));        
            }

            var fromList = new List<FromListItem>(referencedNodes.Distinct().OrderBy(r => r.ForceInnerJoin ? 1 : r.Node.Order).ThenBy(r => r.Node.Level));

            var currentIndex = fromList.Count-1;
            
            while (currentIndex > 0)
            {
                var currentNode = fromList[currentIndex].Node as TableContextNode;
                if (currentNode == null) break;
                var depNodeExist = fromList.Exists(r => r.Node.Index == currentNode.Origin.Index);
                if (!depNodeExist) 
                    fromList.Insert(currentIndex, new FromListItem(currentNode.Origin, currentNode.JoinType == TableJoinType.InnerJoin));

                currentIndex--;
            }

            if (fromList[0].Node != baseNode)
            {
                baseNode = fromList[0].Node;
            }

            fromList.RemoveAll(r => r.Node == baseNode);

            return new QueryContext
            {
                SelectColumns = selectColumns.ToArray(),
                OrderByClause = _ordering,
                Predicates = SelectModifiers,
                BaseNode = baseNode,
                JoinedNodes = fromList.Select(r => new TableJoinDescription(r)).ToArray(),
                Filter = filter
            };
        }

        private List<ContextColumn> GetSelectColumnsFromTemplate()
        {
            var selectColumns = new List<ContextColumn>();

            foreach (var item in SelectTemplate)
            {
                var splitPath = PathHelper.SplitLastInPathString(item.Key);
                var node = string.IsNullOrEmpty(splitPath.Item1)?this:FindByPath(splitPath.Item1);
                var member = node.MapEntry.GetMemberByName(splitPath.Item2);
                var col = node.MapEntry.GetColumnByMember(member);


                selectColumns.Add(GetContextColumn(node, col, "", item.Value));
            }

            return selectColumns;
        }

        private void AddSelectColumnsFromNode(ITableContextNode node, List<ContextColumn> list, string joinAlias)
        {
            foreach (var column in node.Table.Columns.Where(r => !r.ExcludeFromSelect))
            {
                list.Add(GetContextColumn(node, column, joinAlias));
            }
        }

        protected static ContextColumn GetContextColumn(ITableContextNode node, DbColumn column, string joinAlias, string alias = null)
        {
            ValueAdapter adapter = null;
            if (node.MapEntry != null)
            {
                adapter = node.MapEntry.GetAdapter(column);

            }
            var givenName = column.ColumnName;

            var selCol = new ContextColumn(node, column, adapter);
            
            if (column.ForeignkeyRelationship != null && column.ForeignkeyRelationship.IsLookupRelationship)
            {
                selCol.Node = node.Nodes["LOOKUP~" + column.ColumnName];
                selCol.Column = column.ForeignkeyRelationship.LookupColumn;
            }

            var aliasPart = "";
            
            if (!string.IsNullOrEmpty(alias))
            {
                aliasPart = $"{alias}";
            }
            else if (!string.IsNullOrEmpty(joinAlias))
            {
                aliasPart = $"{joinAlias}.{givenName}";
            }
            else if (givenName != selCol.Column.ColumnName)
            {
                aliasPart = $"{givenName}";
            }
            selCol.ColumnAlias = aliasPart;

            return selCol;
        }
        
        public void ApplyFilter(ExpressionGraph filter)
        {

            var filterGraph = new FilterGraph
            {
                Root = ProcessFilter(filter.Root) as BinaryOperand
            };
            RootFilter = filterGraph;

        }

        public FilterGraph GetFilter()
        {
            return RootFilter;
        }

        public void ApplyOrdering(Dictionary<string, Ordering> ordering)
        {
            _ordering = new Dictionary<ContextColumn, Ordering>();
            foreach (var item in ordering)
            {
                var splitPath = PathHelper.SplitLastInPathString(item.Key);
                var node = FindByPath(splitPath.Item1);
                if (node == null) throw new CoPilotConfigurationException("No context found!");
                string lookup = null;
                if (SelectTemplate != null)
                {
                    if (string.IsNullOrEmpty(item.Key) || item.Key.Equals("1", StringComparison.Ordinal))
                    {
                        lookup = SelectTemplate.First().Key;
                    }
                    else
                    {
                        lookup = SelectTemplate.Where(r => r.Value.Equals(splitPath.Item2, StringComparison.Ordinal))
                                .Select(r => r.Key).SingleOrDefault();
                    }
                }
                if(lookup == null)
                    lookup = splitPath.Item2;
               
                var member = node.MapEntry.GetMemberByName(lookup);
                if (member == null) throw new CoPilotConfigurationException("No member found!");
                var col = node.MapEntry.GetColumnByMember(member);
                if (col != null)
                {
                    _ordering.Add(GetContextColumn(node, col, null), item.Value);
                }
            }
        }

        public void SetSelectModifiers(SelectModifiers modifiers)
        {
            SelectModifiers = modifiers;
        }
        public bool Exist(string path)
        {
            return FindByPath(path) != null;
        }

        public ITableContextNode FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return this;
            
            var splitPath = path.Split('.');

            ITableContextNode current = this;
            foreach (var part in splitPath)
            {
                if (!current.Nodes.ContainsKey(part)) return null;
                current = current.Nodes[part];
            }

            return current;
  
        }

        public ITableContextNode[] GetAllNodesInPath(string path, ITableContextNode fromNode = null)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var nodes = new List<ITableContextNode>();

            var splitPath = path.Split('.');

            ITableContextNode current = fromNode??this;
            foreach (var part in splitPath)
            {
                if (!current.Nodes.ContainsKey(part)) return null;
                current = current.Nodes[part];
                nodes.Add(current);
            }

            return nodes.ToArray();
        }

        public ITableContextNode[] GetAllNodes()
        {
            return _nodeIndex.Values.ToArray();
        }

        public ITableContextNode GetNodeByIndex(int idx)
        {
            return _nodeIndex.ContainsKey(idx) ? _nodeIndex[idx] : null;
        }
        
        private IExpressionOperand ProcessFilter(IExpressionOperand sourceOp)
        {
            if(sourceOp is UnsupportedOperand) throw new CoPilotUnsupportedException(sourceOp.ToString());

            var bop = sourceOp as BinaryOperand;
            if (bop != null)
            {
                var bin = new BinaryOperand(
                    ProcessFilter(bop.Left), 
                    ProcessFilter(bop.Right), 
                    bop.Operator
                );

                var memberOperand = bin.Left as MemberExpressionOperand;
                ValueOperand matchingVop;
                if (memberOperand == null)
                {
                    memberOperand = bin.Right as MemberExpressionOperand;
                    matchingVop = bin.Left as ValueOperand;
                }
                else
                {
                    matchingVop = bin.Right as ValueOperand;
                }

                if (matchingVop != null && memberOperand?.ColumnReference.Adapter != null)
                {
                    //special case for enums
                    var member = memberOperand.ColumnReference.Node?.MapEntry?.GetMappedMember(memberOperand.ColumnReference.Column);
                    if (member != null && member.MemberType.GetTypeInfo().IsEnum)
                    {
                        matchingVop.Value = Enum.ToObject(member.MemberType, matchingVop.Value);
                    }
                    matchingVop.Value = memberOperand.ColumnReference.Adapter.Invoke(MappingTarget.Database, matchingVop.Value);
                }
                return bin;
            }

            var mop = sourceOp as MemberExpressionOperand;
            if (mop != null)
            {
                ProcessMemberExpression(mop);
                return mop;
            }

            var vop = sourceOp as ValueOperand;
            if (vop != null)
            {
                return new ValueOperand(vop.ParamName, vop.Value);
            }
            
            return new NullOperand();
        }

        private void ProcessMemberExpression(MemberExpressionOperand memberExpression)
        {
            var path = memberExpression.Path;
            var splitPath = PathHelper.SplitLastInPathString(path);

            if (!string.IsNullOrEmpty(splitPath.Item1) && !Exist(splitPath.Item1))
            {
                AddPath(splitPath.Item1, false);
            }

            TableMapEntry mapEntry;
            ITableContextNode node = this;
            if (string.IsNullOrEmpty(splitPath.Item1))
            {
                mapEntry = MapEntry;
            }
            else
            {
                node = FindByPath(splitPath.Item1);
                mapEntry = node.MapEntry;
            }
            var member = mapEntry.GetMemberByName(splitPath.Item2);
            
            var adapter = mapEntry.GetAdapter(member);
            var col = mapEntry.GetColumnByMember(member);
            if (col == null && !member.MemberType.IsSimpleValueType())
            {
                var rel = mapEntry.GetRelationshipByMember(member);
                if (rel != null)
                {
                    col = member.MemberType.IsReference() ? rel.ForeignKeyColumn : rel.PrimaryKeyColumn;
                }
                
            }
            if (col == null) throw new CoPilotRuntimeException("Cannot map expression to a column!");
            if (col.ForeignkeyRelationship != null && col.ForeignkeyRelationship.IsLookupRelationship)
            {
                node = GetOrCreateLookupNode(node, col);
                col = col.ForeignkeyRelationship.LookupColumn;

            } else if (col.IsPrimaryKey && node.Origin != null)
            {
                node = node.Origin;
                var newCol = node.Table.Columns.FirstOrDefault(r => r.IsForeignKey && r.ForeignkeyRelationship.PrimaryKeyColumn.Equals(col));
                col = newCol;
            }

            if(col == null) throw new CoPilotRuntimeException("Column could not found!");

            memberExpression.ColumnReference = new ContextColumn(node, col, adapter);
        }

        protected void BuildFromMemberExpressions(Dictionary<string, MemberExpression> members)
        {
            foreach (var key in members.Keys)
            {
                var memberExpression = members[key];

                if (memberExpression == null)
                {
                    throw new NotSupportedException("Selector object can only contain direct member access!");
                }
                var path = PathHelper.RemoveFirstElementFromPathString(memberExpression.ToString());
                BuildFromPath(key, path);
            } 
        }

        protected void BuildFromPath(string key, string path)
        {
            var splitPath = PathHelper.SplitLastInPathString(path);
            if (!string.IsNullOrEmpty(splitPath.Item1))
            {
                AddPath(splitPath.Item1, false);
            }

            CreateLookupNodeIfNotExist(splitPath.Item1, splitPath.Item2);

            SelectTemplate.Add(path, key);
        }

        private void CreateLookupNodesIfNotExist(ITableContextNode node)
        {
            var lookupColumns = node.Table.Columns.Where(r => r.ForeignkeyRelationship != null && r.ForeignkeyRelationship.IsLookupRelationship);
            foreach (var lookupColumn in lookupColumns)
            {
                GetOrCreateLookupNode(node, lookupColumn);
            }
        }

        private void CreateLookupNodeIfNotExist(string path, string memberName)
        {
            var node = (string.IsNullOrEmpty(path) ? this : FindByPath(path));
            var member = node.MapEntry.GetMemberByName(memberName);
            var col = node.Table.GetColumnByMember(member);
            if (col?.ForeignkeyRelationship != null && col.ForeignkeyRelationship.IsLookupRelationship)
            {
                GetOrCreateLookupNode(node, col);
            }
        }

        public override string ToString()
        {
            return $"T{Index} ({Table})";
        }

        private ITableContextNode GetOrCreateLookupNode(ITableContextNode source, DbColumn column)
        {
            var alias = "LOOKUP~" + column.ColumnName; 
            if (source.Nodes.ContainsKey(alias))
            {
                return source.Nodes[alias];
            }
            
            var lookupNode = new TableContextNode(source, column.ForeignkeyRelationship, false, ++_index, null);
            source.Nodes.Add(alias, lookupNode);
            _nodeIndex.Add(_index, lookupNode);
            return lookupNode;
            
        }

        public OperationContext Insert(ITableContextNode node, object entity, Dictionary<string, object> additionalValues = null)
        {
            var mappedColumns = node.MapEntry.GetMappedColumns();
            var context = new OperationContext { Node = node };
            var index = 1;
            var keyIndex = 0;

            foreach (var col in mappedColumns.Keys)
            {
                object value = null;
                var member = mappedColumns[col];
                if (member != null)
                {
                    value = member.GetValue(entity);
                    var adapter = context.Node.MapEntry.GetAdapter(col);
                    if (adapter != null)
                    {
                        value = adapter.Invoke(MappingTarget.Database, value);
                    }
                }
                else if (col.IsForeignKey)
                {
                    value = node.MapEntry.GetValueForColumn(entity, col);
                    //var keyFor = node.MapEntry.GetKeyForMember(col.ForeignkeyRelationship);
                    //var keyForInstance = keyFor?.GetValue(entity);
                    //if (keyForInstance != null)
                    //{
                    //    var map = _model.GetTableMap(keyForInstance.GetType());
                    //    value = map.GetValueForColumn(keyForInstance, col.ForeignkeyRelationship.PrimaryKeyColumn);
                    //}
                }
                
                if (value == null && !col.IsNullable && col.DefaultValue == null)
                {
                    if (additionalValues != null && additionalValues.ContainsKey(col.AliasName))
                    {
                        value = additionalValues[col.AliasName];
                    }
                    else
                    {
                        throw new CoPilotDataException($"No value specified for required column '{col.ColumnName}'.");
                    }
                }

                if (value != null || col.DefaultValue != null)
                {
                    string paramName;
                    if (col.IsPrimaryKey && col.DefaultValue?.Value == null)
                    {
                        if (value == null || value.Equals(ReflectionHelper.GetDefaultValue(value.GetType())))
                        {
                            continue;
                        }
                        paramName = "@key";
                        keyIndex++;

                        if (keyIndex > 1)
                        {
                            paramName += keyIndex;
                        }
                    }
                    else
                    {
                        paramName = $"@param{(index++)}";
                    }
                    var param = new DbParameter(paramName, col.DataType, col.DefaultValue?.CreateDefaultValue(), col.IsNullable);
                    if (value != null)
                    {
                        context.Args.Add(param.Name, value);
                    }
                    context.Columns.Add(col, param);
                }
            }
            return context;
        }

        public OperationContext InsertUsingTemplate(ITableContextNode node, object entity)
        {
            var props = entity.GetType().GetClassMembers();

            var mappedColumns = node.Table.GetColumnsByAlias(props);
            var context = new OperationContext { Node = node };
            var index = 1;
            foreach (var col in mappedColumns.Keys)
            {
                object value = null;
                var member = mappedColumns[col];
                if (member != null)
                {
                    value = member.GetValue(entity);
                    var adapter = context.Node.MapEntry.GetAdapter(col);
                    if (adapter != null)
                    {
                        value = adapter.Invoke(MappingTarget.Database, value);
                    }
                }
                else
                {
                    if (!col.IsNullable && col.DefaultValue == null)
                    {
                        throw new CoPilotDataException($"No value specified for required non nullable column '{col.ColumnName}'!");
                    }
                }

                if (value == null && !col.IsNullable && col.DefaultValue == null)
                {
                    throw new CoPilotDataException($"No value specified for required column '{col.ColumnName}'.");
                }

                if (value != null || col.DefaultValue != null)
                {
                    string paramName;
                    if (col.IsPrimaryKey)
                    {
                        if (value == null || value.Equals(ReflectionHelper.GetDefaultValue(value.GetType())))
                        {
                            continue;
                        }
                        paramName = "@key";
                    }
                    else
                    {
                        paramName = $"@param{(index++)}";
                    }
                    var param = new DbParameter(paramName, col.DataType, col.DefaultValue?.CreateDefaultValue(), col.IsNullable);
                    if (value != null)
                    {
                        context.Args.Add(param.Name, value);
                    }
                    context.Columns.Add(col, param);
                }
            }
            return context;
        }

        public OperationContext Delete(ITableContextNode node, object entity)
        {
            var keyIndex = 0;
            var keys = node.Table.GetKeys();
            if (!keys.Any()) throw new CoPilotUnsupportedException("Can only delete a record by key!");

            var ctx = new OperationContext { Node = node };
            foreach (var keyCol in keys)
            {
                var keyMember = node.MapEntry.GetMappedMember(keyCol);
                var keyValue = keyMember.GetValue(entity);

                if (keyValue == null) throw new CoPilotUnsupportedException($"Cannot delete a record that has no key value! (column: {keyCol.ColumnName})");
                var name = "@id";
                keyIndex++;
                if (keyIndex > 1)
                {
                    name += keyIndex;
                }
                var parameter = new DbParameter(name, keyCol.DataType);

                ctx.Columns.Add(keyCol, parameter);
                ctx.Args.Add(parameter.Name, keyValue);
            }
            
            

            

            return ctx;
        }

        public OperationContext Update(ITableContextNode node, object entity, Dictionary<string, object> additionalValues = null)
        {
            var keys = node.Table.GetKeys();
            var keyIndex = 0;

            if (!keys.Any()) throw new CoPilotUnsupportedException("Can only update a record by id!");

            var mappedColumns = node.MapEntry.GetMappedColumns();
            var context = new OperationContext { Node = node };
            
            var index = 1;
            foreach (var col in mappedColumns.Keys)
            {
                object value = null;
                var member = mappedColumns[col];
                if (member != null)
                {
                    value = member.GetValue(entity);
                    var adapter = context.Node.MapEntry.GetAdapter(col);
                    if (adapter != null)
                    {
                        value = adapter.Invoke(MappingTarget.Database, value);
                    }
                }
                else if (col.IsForeignKey)
                {
                    var keyFor = node.MapEntry.GetKeyForMember(col.ForeignkeyRelationship);
                    var keyForInstance = keyFor?.GetValue(entity);
                    if (keyForInstance != null)
                    {
                        var map = Model.GetTableMap(keyForInstance.GetType());
                        value = map.GetValueForColumn(keyForInstance, col.ForeignkeyRelationship.PrimaryKeyColumn);
                    }
                }
               
                if (value == null)
                {
                    if (additionalValues != null && additionalValues.ContainsKey(col.AliasName))
                    {
                        value = additionalValues[col.AliasName];
                    }
                    else
                    {
                        if(!col.IsNullable && col.DefaultValue == null)
                            throw new CoPilotDataException($"No value specified for required column '{col.ColumnName}'.");
                    }
                }

                if (value != null || col.IsNullable)
                {
                    string paramName;
                    if (col.IsPrimaryKey)
                    {
                        var keyMember = node.MapEntry.GetMappedMember(col);
                        var keyValue = keyMember.GetValue(entity);

                        if (keyValue == null)
                            throw new CoPilotUnsupportedException("Cannot update a record that has no key value!");

                        paramName = "@key";
                        keyIndex++;
                        if (keyIndex > 1)
                        {
                            paramName += keyIndex;
                        }
                    }
                    else
                    {
                        paramName = $"@param{index++}";
                    }
                    var param = new DbParameter(paramName, col.DataType, null, col.IsNullable);
                    
                    context.Args.Add(param.Name, value);
                    context.Columns.Add(col, param);
                }
            }
            if (keyIndex != keys.Length)
            {
                throw new CoPilotUnsupportedException("Cannot update a record that has no key value!");
            }
            return context;
        }

        public OperationContext Patch(ITableContextNode node, object entity)
        {
            var props = entity.GetType().GetClassMembers();
            var matchedProps =
                node.MapEntry.EntityType
                    .GetClassMembers()
                    .Join(props, d => d.Name.ToLower(), e => e.Name.ToLower(), (d, e) => new {entityMember = d, dtoMember = e})
                    .ToArray();

            if (matchedProps.Length != props.Length)
            {
                throw new CoPilotDataException("Unable to match all members of the dto object with members of the entity type!");
            }

            var context = new OperationContext { Node = node };
            var index = 1;
            foreach (var member in matchedProps)
            {
                var col = node.MapEntry.GetColumnByMember(member.entityMember);
                if(col == null) throw new CoPilotConfigurationException($"There's no column mapped to member '{member.entityMember.Name}'");

                var value = member.dtoMember.GetValue(entity);
                
                if (value.GetType() != member.entityMember.MemberType)
                {
                    object convertedValue;
                    ReflectionHelper.ConvertValueToType(member.entityMember.MemberType, value, out convertedValue);
                    value = convertedValue;
                }
               
                var adapter = context.Node.MapEntry.GetAdapter(col);
                if (adapter != null)
                {
                    value = adapter.Invoke(MappingTarget.Database, value);
                }
                    
                string paramName;
                if (col.IsPrimaryKey)
                {
                    if (value == null || value.Equals(ReflectionHelper.GetDefaultValue(value.GetType())))
                    {
                        throw new CoPilotDataException("You have to provide a key value in order to patch an object!");
                    }
                    paramName = "@key";
                }
                else
                {
                    if (value == null && !col.IsNullable)
                    {
                        throw new CoPilotDataException("A null value was provided for a non-nullable column!");
                    }
                    paramName = $"@param{(index++)}";
                }
               
                var param = new DbParameter(paramName, col.DataType, col.DefaultValue?.CreateDefaultValue(),col.IsNullable);
                context.Args.Add(param.Name, value);
                context.Columns.Add(col, param);
            }
            if (!context.Columns.Any(r => r.Key.IsPrimaryKey))
            {
                throw new CoPilotDataException("You have to provide a key value in order to patch an object!");
            }
            
            return context;
        }
    }


    public class TableContext<T> : TableContext where T : class
    {
        public TableContext(DbModel model, params string[] include) : base(model, typeof(T), include){}

        public void ApplySelector<TTarget>(Expression<Func<T, TTarget>> selector) {
            ApplySelector(selector.Body);
        }

        public void ApplySelector(Expression<Func<T,object>> selector)
        {
            ApplySelector(selector.Body);
        }

        private void ApplySelector(Expression selectorBody)
        {
            var memberExpression = selectorBody as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpression = selectorBody as UnaryExpression;
                memberExpression = unaryExpression?.Operand as MemberExpression;
            }
            if (memberExpression != null)
            {
                var path = PathHelper.RemoveFirstElementFromPathString(memberExpression.ToString());

                if (!path.Contains("."))
                {
                    var classMemberInfo = ClassMemberInfo.Create(ExpressionHelper.GetPropertyFromMemberExpression<T>(memberExpression));
                    if (classMemberInfo.MemberType.IsCollection()) throw new CoPilotUnsupportedException("The selector cannot return a collection type!");
                    if (classMemberInfo.MemberType.IsReference())
                    {
                        var dtoMembers = classMemberInfo.MemberType.GetClassMembers();
                        foreach (var memberInfo in dtoMembers)
                        {
                            if (memberInfo.MemberType.IsSimpleValueType())
                            {
                                BuildFromPath(memberInfo.Name, path + "." + memberInfo.Name);
                            }

                        }
                        return;
                    }

                }

                BuildFromPath(memberExpression.Member.Name, path);
                return;
            }

            var members = new Dictionary<string, MemberExpression>();

            var memberInitExpression = selectorBody as MemberInitExpression;
            if (memberInitExpression != null)
            {
                foreach (var binding in memberInitExpression.Bindings.OfType<MemberAssignment>())
                {
                    var member = binding.Expression as MemberExpression;
                    if (member != null)
                    {
                        members.Add(binding.Member.Name, member);
                    }
                    else
                    {
                        throw new CoPilotUnsupportedException("Selector object not supported!");
                    }
                }

            }
            else
            {
                var templateExpression = selectorBody as NewExpression;

                if (templateExpression == null) throw new CoPilotUnsupportedException("Only a new anonymous object with named member references are supported!");

                for (var i = 0; i < templateExpression.Members.Count; i++)
                {
                    memberExpression = templateExpression.Arguments[i] as MemberExpression;
                    if (memberExpression == null)
                    {
                        throw new CoPilotUnsupportedException("Selector object can only contain direct member access!");
                    }
                    members.Add(templateExpression.Members[i].Name, memberExpression);
                }
            }

            
            BuildFromMemberExpressions(members);
        }

        public OperationContext Insert(T entity)
        {
            return Insert(this, entity);
        }
        
        public OperationContext Delete(T entity)
        {
            return Delete(this, entity);
        }

        public OperationContext Update(T entity)
        {
            return Update(this, entity);
        }

        
    }
}