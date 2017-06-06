using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Database.Commands.SqlWriters
{
    public interface ICommonScriptingTasks
    {
        ScriptBlock GetSelectKeysFromChildTableScript(string tableName, string pkCol, string keyCol);
        ScriptBlock SetForeignKeyValueToNullScript(string tableName, string fkCol, string keyCol);
        ScriptBlock WrapInsideIdentityInsertScript(string tableName, ScriptBlock sourceScript);
    }

    public class SqlCommonScriptingTasks : ICommonScriptingTasks
    {
        public ScriptBlock GetSelectKeysFromChildTableScript(string tableName, string pkCol, string keyCol)
        {
            return new ScriptBlock($"SELECT {pkCol} FROM {tableName} WHERE {keyCol} = @key");
        }

        public ScriptBlock SetForeignKeyValueToNullScript(string tableName, string fkCol, string keyCol)
        {
            return new ScriptBlock($"UPDATE {tableName} SET [{fkCol}]=NULL WHERE [{keyCol}] = @key");
        }

        public ScriptBlock WrapInsideIdentityInsertScript(string tableName, ScriptBlock sourceScript)
        {
            sourceScript.WrapInside(
                    $"SET IDENTITY_INSERT {tableName} ON",
                    $"SET IDENTITY_INSERT {tableName} OFF",
                    false);

            return sourceScript;
        }
    }
}
