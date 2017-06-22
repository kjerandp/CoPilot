using System.Collections.Generic;
using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Filtering;

namespace CoPilot.ORM.Database.Commands.Query.Interfaces
{
    public interface IQueryExecutionStrategy
    {
        IEnumerable<object> Execute(ITableContextNode node, FilterGraph filter, DbReader reader);
        IEnumerable<T> Execute<T>(ITableContextNode node, FilterGraph filter, DbReader reader);
    }
}
