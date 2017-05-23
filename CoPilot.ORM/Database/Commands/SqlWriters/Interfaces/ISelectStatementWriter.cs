using CoPilot.ORM.Context.Query;

namespace CoPilot.ORM.Database.Commands.SqlWriters.Interfaces
{
    public interface ISelectStatementWriter
    {
        SqlStatement GetStatement(QuerySegments segments);

    }
}