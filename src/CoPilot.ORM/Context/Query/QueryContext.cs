using System.Collections.Generic;
using System.Linq;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Filtering;

namespace CoPilot.ORM.Context.Query
{
    public struct QueryContext
    {
        public ContextColumn[] SelectColumns { get; internal set; }
        public Dictionary<ContextColumn, Ordering> OrderByClause { get; internal set; }
        public ITableContextNode BaseNode { get; internal set; }
        public TableJoinDescription[] JoinedNodes { get; internal set; }
        public FilterGraph Filter { get; set; }
        public Predicates Predicates { get; internal set; }

        public SqlStatement GetStatement(IQueryBuilder builder, ISelectStatementWriter writer)
        {
            var qs = builder.Build(this);
            var stm = new SqlStatement(writer.GetStatement(qs));
            if (Filter != null)
            {
                stm.Parameters = Filter.Parameters.ToList();
                stm.Args = Filter.Arguments;
            }
            
            return stm;
        }
    }
}