using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Database.Commands.Options;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public interface IDeleteStatementWriter
    {
        SqlStatement GetStatement(OperationContext ctx, ScriptOptions options = null);
    }
}
