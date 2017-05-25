using System.Linq;
using CoPilot.ORM.Context;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.Query.Strategies;

namespace CoPilot.ORM.Database.Commands.Query
{
    public class SqlQueryStrategySelector : IQueryStrategySelector
    {
        public IQueryExecutionStrategy Get(TableContext ctx)
        {
            if (ctx.Predicates != null && ctx.Nodes.Any(r => r.Value.IsInverted))
                return new TempTableJoinStrategy();

            return new DefaultStrategy();
        }
    }
}