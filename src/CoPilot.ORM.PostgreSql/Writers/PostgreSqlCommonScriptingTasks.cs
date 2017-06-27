using System.Linq;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.PostgreSql.Writers
{

    public class PostgreSqlCommonScriptingTasks : ICommonScriptingTasks
    {
        private readonly PostgreSqlProvider _provider;

        public PostgreSqlCommonScriptingTasks(PostgreSqlProvider provider)
        {
            _provider = provider;
        }
        public ScriptBlock GetSelectKeysFromChildTableScript(DbTable table, string pkCol, string keyCol)
        {
            return new ScriptBlock($"SELECT {pkCol.QuoteIfNeeded()} FROM {table.GetAsString()} WHERE {keyCol.QuoteIfNeeded()} = @key");
        }

        public ScriptBlock SetForeignKeyValueToNullScript(DbTable table, string fkCol, string keyCol)
        {
            return new ScriptBlock($"UPDATE {table.GetAsString()} SET {fkCol.QuoteIfNeeded()}=NULL WHERE {keyCol.QuoteIfNeeded()} = @key");
        }

        public ScriptBlock WrapInsideIdentityInsertScript(DbTable table, ScriptBlock sourceScript)
        {
            return sourceScript;
        }

        public ScriptBlock GetModelValidationScript(DbTable dbTable)
        {
            return new ScriptBlock(
                $"select {string.Join(",", dbTable.Columns.Select(r => r.ColumnName.QuoteIfNeeded()))} from {dbTable.GetAsString()} limit 1;",
                $"select * from {dbTable.GetAsString()} limit 1"
            );
        }

        public ScriptBlock UseDatabase(string databaseName)
        {
            throw new CoPilotUnsupportedException("Cannot switch database!");
        }

        public ScriptBlock CreateDatabase(string databaseName)
        {
            var block = new ScriptBlock();

            block.Add($"CREATE DATABASE {databaseName.ToLower()};");

            return block;
        }

        public ScriptBlock DropDatabase(string databaseName, bool autoCloseConnections = true)
        {
            var block = new ScriptBlock();
            if (autoCloseConnections)
            {
                block.Add($"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{databaseName.ToLower()}' AND pid <> pg_backend_pid();");
            }
            block.Add($"DROP DATABASE IF EXISTS {databaseName.ToLower()};");

            return block;
        }

        public ScriptBlock DropCreateDatabase(string databaseName)
        {
            throw new CoPilotUnsupportedException("Cannot drop/create database in a single statement");
        }

        public ScriptBlock CreateStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body)
        {
            return CreateStoredProcedure(name, parameters, null, body, null);
        }

        public ScriptBlock CreateStoredProcedure(string name, DbParameter[] parameters, ScriptBlock declarations, ScriptBlock body, string[] returnList)
        {
            if (string.IsNullOrEmpty(name)) throw new CoPilotUnsupportedException("You need to provide a name for the stored procedure");

            var paramsString = string.Join(", ",
                parameters.Select(_provider.GetParameterAsString));
            var returnString = string.Empty;
            if (returnList != null && returnList.Any())
            {
                returnString = " RETURNS " + string.Join(", ", returnList);
            }

            if (!string.IsNullOrEmpty(paramsString))
            {
                paramsString = $"({paramsString})";
            }

            var script = new ScriptBlock($"CREATE OR REPLACE FUNCTION {name.QuoteIfNeeded()} {paramsString}{returnString} $$");
            
            if (declarations != null)
            {
                script.Add(declarations);
            }
            script.Add("BEGIN");
            script.AddMultiLineText(body.ToString());
            script.AddMultiLineText("END;\n$$ LANGUAGE plpgsql;", false);
            return script;
        }



        public ScriptBlock CreateOrReplaceStoredProcedure(string name, DbParameter[] parameters, ScriptBlock declarations, ScriptBlock body, string[] returnList)
        {
            var script = CreateStoredProcedure(name, parameters, declarations, body, returnList);
            return script;
        }

        public ScriptBlock CreateOrReplaceStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body)
        {
            var script = CreateStoredProcedure(name, parameters, null, body, null);
            return script;
        }

        public ScriptBlock DropStoredProcedure(string name)
        {
            return new ScriptBlock($"DROP FUNCTION IF EXISTS {name.QuoteIfNeeded()};");
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
