using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;
using CoPilot.ORM.Context.Query.Filter;
using CoPilot.ORM.Exceptions;

namespace CoPilot.ORM.Context
{
    public class TableContext : ITableContextNode
    {
        internal TableContext(DbModel model, TableMapEntry map)
        {
            _nodeIndex = new Dictionary<int, ITableContextNode> { { _index, this } };
            Model = model;
            MapEntry = map;
            Index = _index;
            Nodes = new Dictionary<string, TableContextNode>();
            IncludedNodes = new List<TableContextNode>();
            CreateLookupNodesIfNotExist(this);
        }
        internal TableContext(DbModel model, Type baseType, params string[] include)
        {
            _nodeIndex = new Dictionary<int, ITableContextNode> { { _index, this } };
            //_include = include;
            Model = model;
            //SelectTemplate = new Dictionary<string, string>();
            MapEntry = Model.GetTableMap(baseType);

            if (MapEntry == null)
                throw new CoPilotConfigurationException($"'{baseType.Name}' is not mapped!");
            Index = _index;
            Nodes = new Dictionary<string, TableContextNode>();
            IncludedNodes = new List<TableContextNode>();

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
        public SelectTemplate SelectTemplate { get; internal set; }
        public SelectModifiers SelectModifiers { get; private set; }
        protected FilterGraph RootFilter;
        private readonly Dictionary<int, ITableContextNode> _nodeIndex;
        private int _index = 1;
        //private readonly string[] _include;
        public readonly DbModel Model;
        public Dictionary<ContextColumn, Ordering> Ordering;
        public List<TableContextNode> IncludedNodes { get; }      
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
        public ITableContextNode AddPath(string path, bool includeAll = true)
        {
            var splitPaths = path.Split('.');
            ITableContextNode currentNode = this;

            foreach (var part in splitPaths)
            {
                var member = currentNode.MapEntry.GetMemberByName(part);
                if (member.MemberType.IsSimpleValueType()) break;
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

                    currentNode.Nodes.Add(part, newNode);
                    currentNode = newNode;

                    if (includeAll)
                    {
                        CreateLookupNodesIfNotExist(currentNode);
                    }

                    IncludedNodes.Add(newNode);
                }
                
            }

            return currentNode;
        }      
        public FilterGraph GetFilter()
        {
            return RootFilter;
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
        public ITableContextNode GetNodeByIndex(int idx)
        {
            return _nodeIndex.ContainsKey(idx) ? _nodeIndex[idx] : null;
        }
        
        public void ApplyFilter(ExpressionGraph filter)
        {
            var decoder = new FilterExpressionProcessor(this);
            
            RootFilter = decoder.Decode(filter);

        }

        #region order clause processing
        public void ApplyOrdering(Dictionary<string, Ordering> ordering)
        {
            if (SelectTemplate == null)
            {
                SelectTemplate = SelectTemplate.BuildFrom(this);
            }
            const string setName = "Base";

            Ordering = new Dictionary<ContextColumn, Ordering>();
            foreach (var item in ordering)
            {
                ContextColumn col;
                if (string.IsNullOrEmpty(item.Key) || item.Key.Equals("1", StringComparison.Ordinal))
                {
                    col = SelectTemplate.GetColumnsInSet(setName).First();
                }
                else
                {
                    col = SelectTemplate.GetColumn(setName, item.Key);
                }
                
                if (col != null)
                {
                    Ordering.Add(col, item.Value);
                }
            }
        }
        #endregion

        #region lookup node processing
        private void CreateLookupNodesIfNotExist(ITableContextNode node)
        {
            var lookupColumns = node.Table.Columns.Where(r => r.ForeignkeyRelationship != null && r.ForeignkeyRelationship.IsLookupRelationship);
            foreach (var lookupColumn in lookupColumns)
            {
                GetOrCreateLookupNode(node, lookupColumn);
            }
        }

        internal ITableContextNode GetOrCreateLookupNode(ITableContextNode source, DbColumn column)
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
        #endregion

        #region context command operations
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
                        if (!col.IsNullable && col.DefaultValue == null)
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
                    .Join(props, d => d.Name.ToLower(), e => e.Name.ToLower(), (d, e) => new { entityMember = d, dtoMember = e })
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
                if (col == null) throw new CoPilotConfigurationException($"There's no column mapped to member '{member.entityMember.Name}'");

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

                var param = new DbParameter(paramName, col.DataType, col.DefaultValue?.CreateDefaultValue(), col.IsNullable);
                context.Args.Add(param.Name, value);
                context.Columns.Add(col, param);
            }
            if (!context.Columns.Any(r => r.Key.IsPrimaryKey))
            {
                throw new CoPilotDataException("You have to provide a key value in order to patch an object!");
            }

            return context;
        }
        #endregion

        public override string ToString()
        {
            return $"T{Index} ({Table})";
        }
    }


    
}