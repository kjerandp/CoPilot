using CoPilot.ORM.Context.Interfaces;
using CoPilot.ORM.Filtering;

namespace CoPilot.ORM.Database.Commands.Query.Interfaces
{
    public interface ISingleStatementQueryWriter
    {
        SqlStatement CreateStatement(ITableContextNode node, FilterGraph filter, out string[] names);
    }
}
