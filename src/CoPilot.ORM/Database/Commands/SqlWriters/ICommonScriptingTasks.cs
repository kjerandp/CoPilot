using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public interface ICommonScriptingTasks
    {
        ScriptBlock GetSelectKeysFromChildTableScript(DbTable table, string pkCol, string keyCol);
        ScriptBlock SetForeignKeyValueToNullScript(DbTable table, string fkCol, string keyCol);
        ScriptBlock WrapInsideIdentityInsertScript(DbTable table, ScriptBlock sourceScript);
        ScriptBlock GetModelValidationScript(DbTable dbTable);
    }
}
