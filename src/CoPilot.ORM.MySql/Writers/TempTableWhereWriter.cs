using System.Collections.Generic;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.Scripting;
using CoPilot.ORM.Database.Commands;
using System.Linq;

namespace CoPilot.ORM.MySql.Writers
{
    public class TempTableWhereWriter : ISingleStatementQueryWriter
    {
        private readonly ISelectStatementBuilder _builder;
        private readonly ISelectStatementWriter _writer;

        public TempTableWhereWriter(ISelectStatementBuilder builder, ISelectStatementWriter writer)
        {
            _builder = builder;
            _writer = writer;
        }
        
        public SqlStatement CreateStatement(ITableContextNode node, FilterGraph filter, out string[] names)
        {
            var tempTables = new List<string>(3);
            var ctx = node.Context;
            var q = ctx.GetQueryContext(node, filter);
            var stm = new SqlStatement(GetScript(q, tempTables));
            if (q.Filter != null)
            {
                stm.Parameters.AddRange(q.Filter.Parameters);
                stm.Args = q.Filter.Arguments;
            }
            var namesList = new List<string> { node.Path };

            AddContextNodeQueries(node, stm, namesList, tempTables);
            
            foreach (var tempTable in tempTables)
            {
                stm.Script.Append(new ScriptBlock($"\nDROP TABLE IF EXISTS {tempTable};"));
            }
            names = namesList.ToArray();
            return stm;
        }

        private ScriptBlock GetScript(QueryContext q, List<string> tempTables)
        {
            var segments = _builder.Build(q);
            var tempName = "tmp_" + q.BaseNode.Path.Replace(".", "_");

            if (q.BaseNode.Nodes.Any(r => r.Value.IsInverted))
            {
                segments.AddToSegment(QuerySegment.PreStatement, $"CREATE TEMPORARY TABLE IF NOT EXISTS {tempName} AS (");
                segments.AddToSegment(QuerySegment.PostStatement, ")");
                tempTables.Add(tempName);
            }

            var script = _writer.GetStatement(segments);
            
            if (segments.Exist(QuerySegment.PreStatement))
            {
                script.Add($"\nSELECT * FROM {tempName};");
            }
            script.Add("");
            return script;
        }

        private void AddContextNodeQueries(ITableContextNode parentNode, SqlStatement stm, List<string> names, List<string> tempTables)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;
                if (node.IsInverted)
                {
                    var filter = CreateChildFilterUsingTempTable(node, "tmp_" + parentNode.Path.Replace(".", "_"));
                    var cStm = GetScript(node.GetQueryContext(filter), tempTables);
                    
                    stm.Script.Append(cStm);
                    names.Add(node.Path);
                    
                }

                AddContextNodeQueries(node, stm, names, tempTables);
            }
        }

        private static FilterGraph CreateChildFilterUsingTempTable(TableContextNode node, string tempTableName)
        {
            var filter = new FilterGraph();
            var left = new MemberExpressionOperand(new ContextColumn(node, node.GetTargetKey, null));
            var right = new CustomOperand($"(Select `{node.GetSourceKey.ColumnName}` from {tempTableName})");
            filter.Root = new BinaryOperand(left, right, SqlOperator.In);

            return filter;

        }
    }
}