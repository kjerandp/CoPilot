using CoPilot.ORM.Context;
using CoPilot.ORM.Database.Commands.Query.Interfaces;

namespace CoPilot.ORM.Database.Commands.Query.Strategies
{
    public delegate IQueryExecutionStrategy QueryStrategySelector(TableContext ctx);
}
