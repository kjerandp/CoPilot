using CoPilot.ORM.Context;
using CoPilot.ORM.Database.Commands.Query.Creators;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.Query.Strategies;
using CoPilot.ORM.Database.Providers;
using System.Linq;

namespace CoPilot.ORM.Database.Commands.Query
{
    public delegate IQueryExecutionStrategy QueryStrategySelector(TableContext ctx);

    public class DefaultQueryExecutionStrategy
    {

        private readonly IQueryExecutionStrategy _basic;
        private readonly IQueryExecutionStrategy _secondary;

        public DefaultQueryExecutionStrategy(IDbProvider provider)
        {
            _basic = new SingleStatementStrategy(new RepeatFilterScriptCreator(provider.SelectStatementBuilder, provider.SelectStatementWriter));
            _secondary = new SingleStatementStrategy(provider.SingleStatementQueryWriter);
        }
        public QueryStrategySelector Get()
        {
            return ctx =>
            {
                if (ctx.Nodes.Any(r => r.Value.IsInverted) && (ctx.SelectModifiers != null || ctx.GetFilter() == null))
                    return _secondary;

                return _basic;
            };
        }
    }
}
