using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Providers.MySql.Writers
{

    public class MySqlCommonScriptingTasks : ICommonScriptingTasks
    {
        public ScriptBlock GetSelectKeysFromChildTableScript(DbTable table, string pkCol, string keyCol)
        {
            return new ScriptBlock($"SELECT `{pkCol}` FROM `{table.TableName}` WHERE `{keyCol}` = @key");
        }

        public ScriptBlock SetForeignKeyValueToNullScript(DbTable table, string fkCol, string keyCol)
        {
            return new ScriptBlock($"UPDATE `{table.TableName}` SET `{fkCol}`=NULL WHERE `{keyCol}` = @key");
        }

        public ScriptBlock WrapInsideIdentityInsertScript(DbTable table, ScriptBlock sourceScript)
        {
            return sourceScript;
        }
    }
}
