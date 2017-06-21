using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Database.Commands.Options;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public interface IInsertStatementWriter
    {
        SqlStatement GetStatement(OperationContext ctx, ScriptOptions options);
    }
}
