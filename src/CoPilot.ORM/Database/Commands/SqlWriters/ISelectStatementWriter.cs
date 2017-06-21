using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public interface ISelectStatementWriter
    {
        ScriptBlock GetStatement(QuerySegments segments);

    }
}