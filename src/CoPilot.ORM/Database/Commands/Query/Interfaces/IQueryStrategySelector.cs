using CoPilot.ORM.Context;

namespace CoPilot.ORM.Database.Commands.Query.Interfaces
{
    public interface IQueryStrategySelector
    {
        IQueryExecutionStrategy Get(TableContext ctx);
    }
}
