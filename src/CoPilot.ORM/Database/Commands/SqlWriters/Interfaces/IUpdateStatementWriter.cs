using CoPilot.ORM.Context;
using CoPilot.ORM.Context.Operations;
using CoPilot.ORM.Database.Commands.Options;

namespace CoPilot.ORM.Database.Commands.SqlWriters.Interfaces
{
    public interface IUpdateStatementWriter
    {
        SqlStatement GetStatement(OperationContext ctx, ScriptOptions options = null);
    }
}