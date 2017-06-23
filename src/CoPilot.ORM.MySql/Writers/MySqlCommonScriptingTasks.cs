using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;
using System.Linq;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Exceptions;

namespace CoPilot.ORM.MySql.Writers
{

    public class MySqlCommonScriptingTasks : ICommonScriptingTasks
    {
        private readonly MySqlProvider _provider;

        public MySqlCommonScriptingTasks(MySqlProvider provider)
        {
            _provider = provider;
        }
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

        public ScriptBlock GetModelValidationScript(DbTable dbTable)
        {
            return new ScriptBlock(
                $"select {string.Join(",", dbTable.Columns.Select(r => "`" + r.ColumnName + "`"))} from `{dbTable.TableName}` limit 1;",
                $"select * from `{dbTable.TableName}` limit 1"
            );
        }

        public ScriptBlock UseDatabase(string databaseName)
        {
            var block = new ScriptBlock();

            block.Add($"USE {databaseName.ToLower()};");

            return block;
        }

        public ScriptBlock CreateDatabase(string databaseName)
        {
            var block = new ScriptBlock();

            block.Add($"CREATE DATABASE IF NOT EXISTS {databaseName.ToLower()};");

            return block;
        }

        public ScriptBlock DropDatabase(string databaseName, bool autoCloseConnections = true)
        {
            var block = new ScriptBlock();

            block.Add($"DROP DATABASE IF EXISTS {databaseName.ToLower()};");

            return block;
        }

        public ScriptBlock DropCreateDatabase(string databaseName)
        {
            var block = UseDatabase(_provider.GetSystemDatabaseName());

            block.Append(DropDatabase(databaseName));
            block.Append(CreateDatabase(databaseName));

            return block;
        }

        public ScriptBlock CreateStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body)
        {
            if (string.IsNullOrEmpty(name)) throw new CoPilotUnsupportedException("You need to provide a name for the stored procedure");

            var paramsString = string.Join(", ",
                parameters.Select(_provider.GetParameterAsString));

            if (!string.IsNullOrEmpty(paramsString))
            {
                paramsString = $"({paramsString})";
            }

            var script = new ScriptBlock($"CREATE PROCEDURE {name} {paramsString}", "BEGIN");
            script.AddMultiLineText(body.ToString());
            script.AddMultiLineText("END;", false);
            return script;
        }

        public ScriptBlock CreateOrReplaceStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body)
        {
            var script = DropStoredProcedure(name);
            script.Append(CreateStoredProcedure(name, parameters, body));
            return script;
        }

        public ScriptBlock DropStoredProcedure(string name)
        {
            return new ScriptBlock($"DROP PROCEDURE IF EXISTS {name};");
        }

        public ScriptBlock CreateTableIfNotExists(DbTable table, CreateOptions options = null)
        {
            return CreateTable(table, options);
        }

        public ScriptBlock CreateTable(DbTable table, CreateOptions options)
        {
            options = options ?? CreateOptions.Default();
            return _provider.CreateStatementWriter.GetStatement(table, options).Script;
        }
    }
}
