using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Filtering;

namespace CoPilot.ORM.Database.Commands.Query.Interfaces
{
    public interface IQueryScriptCreator
    {
        SqlStatement CreateStatement(ITableContextNode node, FilterGraph filter, out string[] names);
    }
}
