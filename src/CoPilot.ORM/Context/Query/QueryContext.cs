using System.Collections.Generic;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context.Interfaces;
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
    }
}