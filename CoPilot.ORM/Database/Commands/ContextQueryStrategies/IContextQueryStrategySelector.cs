using System.Linq;
using CoPilot.ORM.Context;

namespace CoPilot.ORM.Database.Commands.ContextQueryStrategies
{
    public interface IContextQueryStrategySelector
    {
        IContextQueryStrategy Get(TableContext ctx);
    }

    public class SqlDefaultContextQuerySelector : IContextQueryStrategySelector
    {
        public IContextQueryStrategy Get(TableContext ctx)
        {
            if (ctx.Predicates != null && ctx.Nodes.Any(r => r.Value.IsInverted))
                return new ContextQueryTempTableJoinStrategy();

            return new ContextQueryDefaultStrategy();
        }
    }
}
