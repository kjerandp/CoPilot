using System;
using System.Linq;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.SqlServer.Writers
{
    public class SqlCommonScriptingTasks : ICommonScriptingTasks
    {
        private readonly SqlServerProvider _provider;


        public SqlCommonScriptingTasks(SqlServerProvider provider)
        {
            _provider = provider;
        }

        public ScriptBlock GetSelectKeysFromChildTableScript(DbTable table, string pkCol, string keyCol)
        {
            return new ScriptBlock($"SELECT {pkCol} FROM {table.GetAsString()} WHERE {keyCol} = @key");
        }

        public ScriptBlock SetForeignKeyValueToNullScript(DbTable table, string fkCol, string keyCol)
        {
            return new ScriptBlock($"UPDATE {table.GetAsString()} SET [{fkCol}]=NULL WHERE [{keyCol}] = @key");
        }

        public ScriptBlock WrapInsideIdentityInsertScript(DbTable table, ScriptBlock sourceScript)
        {
            sourceScript.WrapInside(
                    $"SET IDENTITY_INSERT {table.GetAsString()} ON",
                    $"SET IDENTITY_INSERT {table.GetAsString()} OFF",
                    false);

            return sourceScript;
        }

        public ScriptBlock GetModelValidationScript(DbTable dbTable)
        {
            return new ScriptBlock(
                $"select top 1 {string.Join(",", dbTable.Columns.Select(r => "[" + r.ColumnName + "]"))} from {dbTable.GetAsString()}",
                $"select top 1 * from {dbTable.GetAsString()}");
        }

        public ScriptBlock UseDatabase(string databaseName)
        {
            var block = new ScriptBlock();

            block.Add($"USE {databaseName}");

            return block;
        }

        public ScriptBlock CreateDatabase(string databaseName)
        {
            var block = new ScriptBlock();

            block.Add($"CREATE DATABASE {databaseName}");

            return block;
        }

        public ScriptBlock DropDatabase(string databaseName, bool autoCloseConnections = true)
        {
            var block = new ScriptBlock();
            if (autoCloseConnections)
            {
                block.Add("DECLARE @SQL varchar(max)");
                block.Add("SELECT @SQL = COALESCE(@SQL,'') + 'Kill ' + Convert(varchar, SPId) + ';'");
                block.Add("FROM MASTER..SysProcesses");
                block.Add($"WHERE DBId = DB_ID({(_provider.UseNationalCharacterSet ? "N" : "")}'{databaseName}') AND SPId <> @@SPId");
                block.Add("EXEC(@SQL)");
            }
            block.Add($"DROP DATABASE {databaseName}");

            return block;
        }

        public ScriptBlock DropCreateDatabase(string databaseName)
        {
            var block = UseDatabase(_provider.GetSystemDatabaseName());

            block.Append(Go());

            block.Append(If().Exists().Database(databaseName).Then(r => r.DropDatabase(databaseName)).End());

            block.Add();
            block.Append(CreateDatabase(databaseName));

            return block;
        }

        public ScriptBlock CreateTable(DbTable table, CreateOptions options)
        {
            options = options ?? CreateOptions.Default();
            return _provider.CreateStatementWriter.GetStatement(table, options).Script;
        }

        public ScriptBlock CreateTableIfNotExists(DbTable table, CreateOptions options = null)
        {
            var createScript = CreateTable(table, options);
            var block = If().NotExists().Table(table.TableName).Then(createScript).End();
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

            var script = new ScriptBlock($"CREATE PROCEDURE {name.QuoteIfNeeded()} {paramsString}", "AS", "BEGIN");
            script.AddMultiLineText(body.ToString());
            script.AddMultiLineText("END", false);
            return script;
        }

        public ScriptBlock CreateOrReplaceStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body)
        {
            var script = If().Exists().StoredProcedure(name).Then(DropStoredProcedure(name)).End();
            script.Append(Go());
            script.Append(CreateStoredProcedure(name, parameters, body));
            return script;
        }

        public ScriptBlock DropStoredProcedure(string name)
        {
            return new ScriptBlock($"DROP PROCEDURE {name.QuoteIfNeeded()}");
        }

        private static ScriptBlock Go(int times = 0)
        {
            var block = new ScriptBlock();
            block.Add("GO" + (times > 0 ? " " + times : ""), "");
            return block;
        }

        #region Classes for fluent typing of conditional statements
        public IfCondition If()
        {
            var block = new ScriptBlock();
            return new IfCondition(this, block, "IF");
        }

        public class IfCondition
        {
            private readonly SqlCommonScriptingTasks _scriptBuilder;
            private readonly ScriptBlock _block;
            private string _currentTextLine;

            internal IfCondition(SqlCommonScriptingTasks scriptBuilder, ScriptBlock block, string currentTextLine)
            {
                _scriptBuilder = scriptBuilder;
                _block = block;
                _currentTextLine = currentTextLine;
            }

            public ThenBlock IsTrue(string expression)
            {
                _currentTextLine += $"({expression})";
                _block.Add(_currentTextLine);
                return new ThenBlock(_scriptBuilder, _block);
            }

            public ThenBlock IsFalse(string expression)
            {
                _currentTextLine += $" NOT ({expression})";
                _block.Add(_currentTextLine);
                return new ThenBlock(_scriptBuilder, _block);
            }

            public IfConditionToEvaluate Exists()
            {
                _currentTextLine += " EXISTS";
                return new IfConditionToEvaluate(_scriptBuilder, _block, _currentTextLine);
            }

            public IfConditionToEvaluate NotExists()
            {
                _currentTextLine += " NOT EXISTS";
                return new IfConditionToEvaluate(_scriptBuilder, _block, _currentTextLine);
            }

            public class IfConditionToEvaluate
            {
                private readonly SqlCommonScriptingTasks _scriptBuilder;
                private readonly ScriptBlock _block;
                private string _currentTextLine;

                public IfConditionToEvaluate(SqlCommonScriptingTasks scriptBuilder, ScriptBlock block, string currentTextLine)
                {
                    _scriptBuilder = scriptBuilder;
                    _block = block;
                    _currentTextLine = currentTextLine;
                }

                public ThenBlock Database(string databaseName)
                {
                    _currentTextLine += $"(select top 1 * from sys.databases where name='{databaseName}')";
                    _block.Add(_currentTextLine);

                    return new ThenBlock(_scriptBuilder, _block);
                }
                public ThenBlock Table(string table)
                {
                    _currentTextLine += $"(SELECT top 1 * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table}')";
                    _block.Add(_currentTextLine);

                    return new ThenBlock(_scriptBuilder, _block);
                }
                public ThenBlock Column(string table, string column)
                {
                    _currentTextLine += $"(SELECT top 1 * FROM sys.columns WHERE Name = {(_scriptBuilder._provider.UseNationalCharacterSet ? "N" : "")}'{column}' AND Object_ID = Object_ID(N'{table}')";
                    _block.Add(_currentTextLine);

                    return new ThenBlock(_scriptBuilder, _block);
                }
                public ThenBlock Query(string query)
                {
                    _currentTextLine += $"({query})";
                    _block.Add(_currentTextLine);

                    return new ThenBlock(_scriptBuilder, _block);
                }
                public ThenBlock TableData(string tableName)
                {
                    _currentTextLine += $"(SELECT TOP 1 * FROM {tableName})";
                    _block.Add(_currentTextLine);

                    return new ThenBlock(_scriptBuilder, _block);
                }

                public ThenBlock StoredProcedure(string name)
                {
                    _currentTextLine += $"(SELECT top 1 * FROM sys.objects WHERE object_id = OBJECT_ID({(_scriptBuilder._provider.UseNationalCharacterSet ? "N" : "")}'{name}'))";
                    _block.Add(_currentTextLine);

                    return new ThenBlock(_scriptBuilder, _block);
                }
            }

            public class ThenBlock
            {
                private readonly SqlCommonScriptingTasks _scriptBuilder;
                private readonly ScriptBlock _block;


                public ThenBlock(SqlCommonScriptingTasks scriptBuilder, ScriptBlock block)
                {
                    _scriptBuilder = scriptBuilder;
                    _block = block;

                }

                public EndOrElse Then(Func<SqlCommonScriptingTasks, ScriptBlock> func)
                {

                    _block.Add("BEGIN");
                    _block.Add(func.Invoke(_scriptBuilder));
                    _block.Add("END");
                    return new EndOrElse(_scriptBuilder, _block);

                }

                public EndOrElse Then(ScriptBlock block, bool append = false)
                {
                    _block.Add("BEGIN");

                    if (append) _block.Append(block);
                    else _block.Add(block);

                    _block.Add("END");
                    return new EndOrElse(_scriptBuilder, _block);
                }

                public EndOrElse Then(string expression)
                {
                    _block.Add("BEGIN");
                    _block.AddMultiLineText(expression);
                    _block.Add("END");
                    return new EndOrElse(_scriptBuilder, _block);
                }
            }

            public class EndOrElse
            {
                private readonly SqlCommonScriptingTasks _scriptBuilder;
                private readonly ScriptBlock _block;

                public EndOrElse(SqlCommonScriptingTasks scriptBuilder, ScriptBlock block)
                {
                    _scriptBuilder = scriptBuilder;
                    _block = block;
                }

                public ScriptBlock End()
                {
                    return _block;
                }

                public IfCondition ElseIf()
                {

                    return new IfCondition(_scriptBuilder, _block, "ELSE IF");
                }

                public ScriptBlock Else(Func<SqlCommonScriptingTasks, ScriptBlock> func)
                {
                    _block.Add("ELSE");
                    _block.Add("BEGIN");
                    _block.Add(func.Invoke(_scriptBuilder));
                    _block.Add("END");

                    return _block;
                }

                public ScriptBlock Else(ScriptBlock block, bool append = false)
                {
                    _block.Add("BEGIN");

                    if (append) _block.Append(block);
                    else _block.Add(block);

                    _block.Add("END");
                    return _block;
                }

                public ScriptBlock Else(string expression)
                {
                    _block.Add("ELSE");
                    _block.Add("BEGIN");
                    _block.AddMultiLineText(expression);
                    _block.Add("END");
                    return _block;
                }

            }
        }

        #endregion
    }
}
