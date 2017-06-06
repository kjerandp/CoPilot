using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public interface ICreateStatementWriter
    {
        SqlStatement GetStatement(DbTable table, CreateOptions options);
    }
}
