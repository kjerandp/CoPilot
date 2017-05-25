using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Database.Commands.SqlWriters.Interfaces
{
    public interface ISelectStatementWriter
    {
        ScriptBlock GetStatement(QuerySegments segments);

    }
}