using System.Linq;
using CoPilot.ORM.Context;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.Query.Strategies;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;

namespace CoPilot.ORM.Database.Commands.Query
{
    public class SqlQueryStrategySelector : IQueryStrategySelector
    {
        private readonly IQueryExecutionStrategy _default;
        private readonly IQueryExecutionStrategy _secondary;

        public SqlQueryStrategySelector(IQueryBuilder builder, ISelectStatementWriter writer)
        {
            _default = new RepeatFilterStrategy(builder, writer);
            _secondary = new TempTableJoinStrategy(builder, writer);
        }
        public IQueryExecutionStrategy Get(TableContext ctx)
        {
            if (ctx.Predicates != null && ctx.Nodes.Any(r => r.Value.IsInverted))
                return _secondary;

            return _default;
        }
    }
}