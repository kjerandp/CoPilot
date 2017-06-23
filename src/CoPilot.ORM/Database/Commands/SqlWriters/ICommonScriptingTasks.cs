using CoPilot.ORM.Database.Commands.Options;
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
        ScriptBlock UseDatabase(string databaseName);
        ScriptBlock CreateDatabase(string databaseName);
        ScriptBlock DropDatabase(string databaseName, bool autoCloseConnections = true);
        ScriptBlock DropCreateDatabase(string databaseName);
        ScriptBlock CreateTable(DbTable table, CreateOptions options);
        ScriptBlock CreateTableIfNotExists(DbTable table, CreateOptions options = null);
        ScriptBlock CreateStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body);
        ScriptBlock CreateOrReplaceStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body);
        ScriptBlock DropStoredProcedure(string name);

    }

}
