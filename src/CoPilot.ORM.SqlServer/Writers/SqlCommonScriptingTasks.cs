﻿using System.Linq;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.SqlServer.Writers
{
    public class SqlCommonScriptingTasks : ICommonScriptingTasks
    {
        public ScriptBlock GetSelectKeysFromChildTableScript(DbTable table, string pkCol, string keyCol)
        {
            return new ScriptBlock($"SELECT {pkCol} FROM [{table.Schema}].[{table.TableName}] WHERE {keyCol} = @key");
        }

        public ScriptBlock SetForeignKeyValueToNullScript(DbTable table, string fkCol, string keyCol)
        {
            return new ScriptBlock($"UPDATE [{table.Schema}].[{table.TableName}] SET [{fkCol}]=NULL WHERE [{keyCol}] = @key");
        }

        public ScriptBlock WrapInsideIdentityInsertScript(DbTable table, ScriptBlock sourceScript)
        {
            sourceScript.WrapInside(
                    $"SET IDENTITY_INSERT [{table.Schema}].[{table.TableName}] ON",
                    $"SET IDENTITY_INSERT [{table.Schema}].[{table.TableName}] OFF",
                    false);

            return sourceScript;
        }

        public ScriptBlock GetModelValidationScript(DbTable dbTable)
        {
            return new ScriptBlock(
                $"select top 1 {string.Join(",", dbTable.Columns.Select(r => "[" + r.ColumnName + "]"))} from [{dbTable.Schema}].[{dbTable.TableName}]",
                $"select top 1 * from [{dbTable.Schema}].[{dbTable.TableName}]");
        }
    }
}