using System.Collections.Generic;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Filtering;

namespace CoPilot.ORM.Database.Commands.ContextQueryStrategies
{
    public interface IContextQueryStrategy
    {
        IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader);
    }
}
