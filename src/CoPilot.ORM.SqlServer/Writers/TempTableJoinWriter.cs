using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.SqlServer.Writers
{
    public class TempTableJoinWriter : ISingleStatementQueryWriter
    {
        private readonly ISelectStatementBuilder _builder;
        private readonly ISelectStatementWriter _writer;

        public TempTableJoinWriter(ISelectStatementBuilder builder, ISelectStatementWriter writer)
        {
            _builder = builder;
            _writer = writer;
        }
       

        public SqlStatement CreateStatement(ITableContextNode node, FilterGraph filter, out string[] names)
        {
            var q = QueryContext.Create(node, filter);
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
            var segments = _builder.Build(q);
            var tempName = q.BaseNode.Path.Replace(".", "_");

            if (q.BaseNode.Nodes.Any(r => r.Value.IsInverted))
            {
                segments.AddToSegment(QuerySegment.PostSelect, $"INTO #{tempName}");
            }
            if (parantNode != null)
            {
                var tn = q.BaseNode as TableContextNode;
                if (tn != null)
                {
                    var join = $"INNER JOIN #{parantNode.Path.Replace(".", "_")} T{parantNode.Index} ON T{q.BaseNode.Index}.{tn.GetTargetKey.ColumnName} = T{parantNode.Index}.{tn.GetSourceKey.ColumnName}";
                    segments.AddToSegment(QuerySegment.PostBaseTable, join);
                }
                
            }
            var script = _writer.GetStatement(segments);

            if (segments.Exist(QuerySegment.PostSelect))
            {
                script.Add($"\nSELECT * FROM #{tempName}\n");
            }
            return script;
        }

        private void AddContextNodeQueries(ITableContextNode parentNode, SqlStatement stm, List<string> names)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var node = rel.Value;
                if (node.IsInverted)
                {
                    stm.Script.Append(GetScript(QueryContext.Create(node), parentNode));
                    names.Add(node.Path);

                }

                AddContextNodeQueries(node, stm, names);
            }
        }
    }
}