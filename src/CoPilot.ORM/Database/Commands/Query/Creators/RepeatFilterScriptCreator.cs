using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Context.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering;

namespace CoPilot.ORM.Database.Commands.Query.Creators
{
    public class RepeatFilterScriptCreator : ISingleStatementQueryWriter
    {
        private readonly ISelectStatementBuilder _builder;
        private readonly ISelectStatementWriter _writer;

        public RepeatFilterScriptCreator(ISelectStatementBuilder builder, ISelectStatementWriter writer)
        {
            _builder = builder;
            _writer = writer;
        }
        
        public SqlStatement CreateStatement(ITableContextNode node, FilterGraph filter, out string[] names)
        {
            var q = QueryContext.Create(node, filter);
            var stm = q.GetStatement(_builder, _writer);
            var namesList = new List<string> { node.Path };

            AddContextNodeQueries(node, stm, filter, namesList);
            names = namesList.ToArray();
            return stm;
        }

        private void AddContextNodeQueries(ITableContextNode parentNode, SqlStatement stm, FilterGraph filter, List<string> names)
        {
            foreach (var rel in parentNode.Nodes.Where(r => !r.Value.Relationship.IsLookupRelationship))
            {
                var ctx = parentNode.Context;
                if (ctx.Nodes.Any(r => r.Value.IsInverted) && (ctx.SelectModifiers != null || ctx.GetFilter() == null)) throw new CoPilotUnsupportedException("This query strategy cannot be used with predicates!");

                var node = rel.Value;
                if (node.IsInverted)
                {
                    var q = QueryContext.Create(node,filter);
                    stm.Script.Add();
                    stm.Script.Append(_writer.GetStatement(_builder.Build(q)));
                    names.Add(node.Path);

                }

                AddContextNodeQueries(node, stm, filter, names);
            }
        }
    }
}