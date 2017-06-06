using CoPilot.ORM.Database.Commands.Query.Strategies;

namespace CoPilot.ORM.Database.Commands.Query.Interfaces
{
    public interface IQueryStrategySelector
    {
        QueryStrategySelector Get();
    }
}
