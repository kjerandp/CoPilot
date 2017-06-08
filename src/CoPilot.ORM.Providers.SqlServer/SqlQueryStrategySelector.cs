using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.Query.Strategies;
using CoPilot.ORM.Database.Commands.SqlWriters;
using System.Linq;
using CoPilot.ORM.Providers.SqlServer.QueryStrategies;

namespace CoPilot.ORM.Providers.SqlServer
{
    public class SqlQueryStrategySelector
    {
        private readonly IQueryExecutionStrategy _default;
        private readonly IQueryExecutionStrategy _secondary;

        public SqlQueryStrategySelector(IQueryBuilder builder, ISelectStatementWriter writer)
        {
            _default = new RepeatFilterStrategy(builder, writer);
            _secondary = new TempTableJoinStrategy(builder, writer);
            //_secondary = new TableVariableJoinStrategy(builder, writer);
        }
        public QueryStrategySelector Get()
        {
            return ctx =>
            {
                if (ctx.Predicates != null && ctx.Nodes.Any(r => r.Value.IsInverted))
                    return _secondary;

                return _default;
            };
        }
    }
}