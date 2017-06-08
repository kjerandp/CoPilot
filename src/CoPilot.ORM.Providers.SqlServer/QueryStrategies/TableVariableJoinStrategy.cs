using System;
using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Mapping.Mappers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Providers.SqlServer.QueryStrategies
{
    public class TableVariableJoinStrategy : IQueryExecutionStrategy, IQueryScriptCreator
    {
        private readonly IDbProvider _provider;

        public TableVariableJoinStrategy(IDbProvider provider)
        {
            _provider = provider;
        }
        public IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader)
        {
            string[] names;

            var stm = CreateStatement(node, filter, out names);

            var response = reader.Query(stm, names.ToArray());

            return ContextMapper.MapAndMerge(node, response.RecordSets);
        }

        public SqlStatement CreateStatement(ITableContextNode node, FilterGraph filter, out string[] names)
        {
            var ctx = node.Context;
            var q = ctx.GetQueryContext(node, filter);
            var stm = new SqlStatement(GetScript(q));
            if (q.Filter != null)
            {
                stm.Parameters.AddRange(q.Filter.Parameters);
                stm.Args = q.Filter.Arguments;
            }
            var namesList = new List<string> { node.Path };

            AddContextNodeQueries(node, stm, namesList);

            names = namesList.ToArray();

            return stm;
        }

        private ScriptBlock GetScript(QueryContext q, ITableContextNode parantNode = null)
        {
            var segments = _provider.QueryBuilder.Build(q);
            var tempName = q.BaseNode.Path.Replace(".", "_");
            var temp = new QuerySegments();
            var script = new ScriptBlock();
            if (q.BaseNode.Nodes.Any(r => r.Value.IsInverted))
            {
                if (q.Filter != null)
                {
                    var referencedTables = q.Filter.MemberExpressions
                            .Where(r => r.ContextColumn.Node.Index != q.BaseNode.Index)
                            .Select(r => " T"+r.ContextColumn.Node.Index+" ").ToArray();

                    var joins = segments.Get(QuerySegment.Joins);
                    var lastJoin = joins.LastOrDefault();
                    while (lastJoin != null)
                    {
                        if (referencedTables.Any(r => lastJoin.IndexOf(r, StringComparison.Ordinal) > 0)) break;

                        joins = joins.Where(r => r != lastJoin).ToArray();
                        lastJoin = joins.LastOrDefault();
                    }
                    temp.AddToSegment(QuerySegment.Joins, joins);
                    temp.AddToSegment(QuerySegment.Filter, segments.Get(QuerySegment.Filter));

                }
                var pk = q.BaseNode.Table.GetSingularKey();
                temp.AddToSegment(QuerySegment.Select, $"T{q.BaseNode.Index}.{pk.ColumnName}");
                temp.AddToSegment(QuerySegment.PreSelect, segments.Get(QuerySegment.PreSelect));
                temp.AddToSegment(QuerySegment.BaseTable, segments.Get(QuerySegment.BaseTable));

                segments.Remove(QuerySegment.PreSelect);
                segments.Remove(QuerySegment.Filter);

                var join = $"INNER JOIN @{tempName} V{q.BaseNode.Index} ON T{q.BaseNode.Index}.{pk.ColumnName} = V{q.BaseNode.Index}.{pk.ColumnName}";
                segments.AddToSegment(QuerySegment.PostBaseTable, join);

                script.Add($"DECLARE @{tempName} TABLE({GetColumnAsString(pk)})");
                script.Add($"INSERT INTO @{tempName}");
                script.Append(_provider.SelectStatementWriter.GetStatement(temp));
                script.Add("");
            }
            if (parantNode != null)
            {
                var tn = q.BaseNode as TableContextNode;
                if (tn != null)
                {
                    var join = $"INNER JOIN @{parantNode.Path.Replace(".", "_")} V{parantNode.Index} ON T{q.BaseNode.Index}.{tn.GetTargetKey.ColumnName} = V{parantNode.Index}.{tn.GetSourceKey.ColumnName}";
                    segments.AddToSegment(QuerySegment.PostBaseTable, join);
                }
            }
            script.Append(_provider.SelectStatementWriter.GetStatement(segments));
            
            
            return script;
        }

        private string GetColumnAsString(DbColumn col)
        {
            var str = $"{col.ColumnName} {_provider.GetDataTypeAsString(col.DataType, col.MaxSize)}";

            //if (col.MaxSize != null && _provider.HasSize(col.DataType))
            //{
            //    str += $"({col.MaxSize})";
            //}
            //else 
            if (col.NumberPrecision != null)
            {
                str += $"({col.NumberPrecision.Scale},{col.NumberPrecision.Precision})";
            }

            str += $" {(!col.IsNullable ? "NOT " : "")}NULL";
            return str;
        }
        

        private void AddContextNodeQueries(ITableContextNode parentNode, SqlStatement stm, List<string> names)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;
                if (node.IsInverted)
                {
                    stm.Script.Append(GetScript(node.GetQueryContext(), parentNode));
                    names.Add(node.Path);

                }

                AddContextNodeQueries(node, stm, names);
            }
        }
    }
}