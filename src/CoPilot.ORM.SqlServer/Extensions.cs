using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Config.Naming;
using CoPilot.ORM.Context;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;
using CoPilot.ORM.Scripting;
using CoPilot.ORM.SqlServer.Writers;

namespace CoPilot.ORM.SqlServer
{
    public static class Extensions
    {
        public static IDb CreateDb(this DbModel model, string connectionString)
        {
            return model.CreateDb(connectionString, new SqlServerProvider());
        }

        public static ScriptBlock InsertIntoTableIfEmpty<T>(this ScriptBuilder sb, ScriptOptions options = null, params T[] entities) where T : class
        {
            options = options ?? ScriptOptions.Default();

            var table = sb.Model.GetTableMap<T>().Table;
            var insertBlock = new ScriptBlock();

            foreach (var entity in entities)
            {
                insertBlock.Append(sb.InsertTable(entity, options));
            }
            if (options.EnableIdentityInsert)
            {
                sb.DbProvider.CommonScriptingTasks.WrapInsideIdentityInsertScript(table, insertBlock);
            }

            var block = sb.If().NotExists().TableData(table.TableName).Then(insertBlock).End();
            return block;
        }

        public static ScriptBlock InsertIntoTableIfEmpty<T>(this ScriptBuilder sb, T obj, ScriptOptions options = null, object additionalValues = null) where T : class
        {
            options = options ?? ScriptOptions.Default();

            var insertBlock = new ScriptBlock();
            var table = sb.Model.GetTableMap<T>().Table;

            insertBlock.Append(sb.InsertTable(obj, options));
            if (options.EnableIdentityInsert)
            {
                sb.DbProvider.CommonScriptingTasks.WrapInsideIdentityInsertScript(table, insertBlock);
            }
            var block = sb.If().NotExists().TableData(table.TableName).Then(insertBlock).End();
            return block;
        }

        public static ScriptBlock InsertIntoTableIfEmpty(this ScriptBuilder sb, DbTable tableDefinition, ScriptOptions options = null, params object[] templateObjects)
        {
            options = options ?? ScriptOptions.Default();

            var insertBlock = new ScriptBlock();
            foreach (var entity in templateObjects)
            {
                insertBlock.Append(sb.InsertTable(tableDefinition, entity, options));
            }
            if (options.EnableIdentityInsert)
            {
                sb.DbProvider.CommonScriptingTasks.WrapInsideIdentityInsertScript(tableDefinition, insertBlock);
            }
            var block = sb.If().NotExists().TableData(tableDefinition.TableName).Then(insertBlock).End();
            return block;
        }

        public static ScriptBlock UseDatabase(this ScriptBuilder sb, string databaseName)
        {
            var block = new ScriptBlock();

            block.Add($"USE {databaseName}");

            return block;
        }

        public static ScriptBlock DropCreateDatabase(this ScriptBuilder sb, string databaseName)
        {
            var block = sb.UseDatabase("master");

            block.Append(sb.Go());

            block.Append(sb.If().Exists().Database(databaseName).Then(r => r.DropDatabase(databaseName)).End());

            block.Add();
            block.Append(sb.CreateDatabase(databaseName));

            return block;
        }

        public static ScriptBlock CreateDatabase(this ScriptBuilder sb, string databaseName)
        {
            var block = new ScriptBlock();

            block.Add($"CREATE DATABASE {databaseName}");

            return block;
        }

        public static ScriptBlock DropDatabase(this ScriptBuilder sb, string databaseName, bool autoCloseConnections = true)
        {
            var block = new ScriptBlock();
            if (autoCloseConnections)
            {
                block.Add("DECLARE @SQL varchar(max)");
                block.Add("SELECT @SQL = COALESCE(@SQL,'') + 'Kill ' + Convert(varchar, SPId) + ';'");
                block.Add("FROM MASTER..SysProcesses");
                block.Add($"WHERE DBId = DB_ID({(sb.DbProvider.UseNationalCharacterSet?"N":"")}'{databaseName}') AND SPId <> @@SPId");
                block.Add("EXEC(@SQL)");
            }
            block.Add($"DROP DATABASE {databaseName}");

            return block;
        }

        public static ScriptBlock DropStoredProcedure(this ScriptBuilder sb, string name)
        {
            return new ScriptBlock($"DROP PROCEDURE {name}");
        }

        public static ScriptBlock Go(this ScriptBuilder sb, int times = 0)
        {
            var block = new ScriptBlock();
            block.Add("GO" + (times > 0 ? " " + times : ""), "");
            return block;
        }

        

        public static ScriptBlock CreateTablesIfNotExists(this ScriptBuilder sb, CreateOptions options = null)
        {
            var toCreate = sb.Model.Tables.ToList();
            var created = new List<DbTable>();
            const int max = 1000;
            var i = 0;
            var block = new ScriptBlock();


            while (!toCreate.All(r => created.Contains(r)) && i < max)
            {
                var table = toCreate.First(r => !created.Contains(r));
                sb.CreateTableAndDependantTables(block, table, created, options);
                i++;
            }
            return block;
        }


        public static ScriptBlock CreateTableIfNotExists<T>(this ScriptBuilder sb, CreateOptions options = null) where T : class
        {
            var table = sb.Model.GetTableMap<T>().Table;

            var createScript = sb.CreateTable<T>(options);
            var block = sb.If().NotExists().Table(table.TableName).Then(createScript).End();
            return block;
        }

        public static ScriptBlock CreateOrReplaceStoredProcedure(this ScriptBuilder sb, string name, DbParameter[] parameters, ScriptBlock body)
        {
            var script = sb.If().Exists().StoredProcedure(name).Then(sb.DropStoredProcedure(name)).End();
            script.Append(sb.Go());
            script.Append(sb.CreateStoredProcedure(name, parameters, body));
            return script;
        }

        public static ScriptBlock CreateStoredProcedureFromQuery<T>(this ScriptBuilder sb, string name, Expression<Func<T, bool>> filter = null, ISingleStatementQueryWriter scriptCreator = null, params string[] include) where T : class
        {
            var ctx = sb.Model.CreateContext<T>(include);

            if (filter != null)
            {
                var decoder = new ExpressionDecoder(sb.DbProvider);
                var expression = decoder.Decode(filter.Body);
                ctx.ApplyFilter(expression);
            }

            return sb.CreateStoredProcedureFromQuery(name, ctx, scriptCreator);
        }

        public static ScriptBlock CreateStoredProcedureFromQuery(this ScriptBuilder sb, string name, TableContext ctx, ISingleStatementQueryWriter scriptCreator = null)
        {
            if (scriptCreator == null)
                scriptCreator = new TempTableJoinWriter(sb.DbProvider.SelectStatementBuilder, sb.DbProvider.SelectStatementWriter);

            var rootFilter = ctx.GetFilter();
            var paramToColumnMap = new Dictionary<string, ContextColumn>();
            var parameters = new List<DbParameter>();

            string[] names;
            var sqlStatement = scriptCreator.CreateStatement(ctx, rootFilter, out names);
            if (rootFilter != null)
            {
                MapParametersToColumns(rootFilter.Root, paramToColumnMap);
            }

            var caseConverter = new SnakeOrKebabCaseConverter(r => r.ToLower());
            var script = sqlStatement.Script.ToString();
            foreach (var p in sqlStatement.Parameters)
            {
                var contextColumn = paramToColumnMap[p.Name];
                var newName = "@" +
                                caseConverter.Convert(
                                    contextColumn.Node.MapEntry.GetMappedMember(contextColumn.Column).Name);
                if (!parameters.Any(r => r.Name.Equals(newName, StringComparison.Ordinal)))
                {
                    var param = new DbParameter(newName, p.DataType, p.DefaultValue, p.CanBeNull, p.IsOutput);
                    if (DbConversionHelper.DataTypeHasSize(contextColumn.Column.DataType))
                    {
                        param.Size = contextColumn.Column.MaxSize;
                    }
                    else if (contextColumn.Column.NumberPrecision != null)
                    {
                        param.NumberPrecision = contextColumn.Column.NumberPrecision;
                    }
                    parameters.Add(param);
                }
                script = script.Replace(p.Name, newName);
            }
            var comment = "This script was autogenerated by CoPilot.";
            if (names.Length > 1)
            {
                comment += $"\n\nRecord sets should be named as follows when executed from CoPilot:\n - {string.Join("\n - ", names)}";
            }
            var scriptBlock = sb.MultiLineComment(comment);
            scriptBlock.AddMultiLineText(script, false);
            return sb.CreateOrReplaceStoredProcedure(name, parameters.ToArray(), scriptBlock);
        }

        public static ScriptBlock CreateTableIfNotExists(this ScriptBuilder sb, DbTable table, CreateOptions options = null)
        {
            var createScript = sb.CreateTable(table, options);
            var block = sb.If().NotExists().Table(table.TableName).Then(createScript).End();
            return block;
        }
        
        public static ScriptBlock CreateStoredProcedure(this ScriptBuilder sb, string name, DbParameter[] parameters, ScriptBlock body)
        {
            if (string.IsNullOrEmpty(name)) throw new CoPilotUnsupportedException("You need to provide a name for the stored procedure");

            var paramsString = string.Join(", ",
                parameters.Select(sb.GetParameterAsString));

            if (!string.IsNullOrEmpty(paramsString))
            {
                paramsString = $"({paramsString})";
            }

            var script = new ScriptBlock($"CREATE PROCEDURE {name} {paramsString}","AS","BEGIN");
            script.AddMultiLineText(body.ToString());
            script.AddMultiLineText("END", false);
            return script;
        }

        private static string GetParameterAsString(this ScriptBuilder sb, DbParameter prm)
        {
            var str = prm.Name + " " + sb.DbProvider.GetDataTypeAsString(prm.DataType, prm.Size);
            if (prm.NumberPrecision != null)
            {
                str += $"({prm.NumberPrecision.Scale},{prm.NumberPrecision.Precision})";
            }
            if (prm.DefaultValue != null)
            {
                str += $" DEFAULT({prm.DefaultValue as string})"; 
            }

            return str;
        }
        

        private static void CreateTableAndDependantTables(this ScriptBuilder sb, ScriptBlock block, DbTable table, List<DbTable> created, CreateOptions options)
        {

            var dependantTables = table.Columns.Where(r => r.IsForeignKey).Select(r => r.ForeignkeyRelationship.PrimaryKeyColumn.Table).Distinct();
            foreach (var dependantTable in dependantTables.Where(r => !created.Contains(r)))
            {
                sb.CreateTableAndDependantTables(block, dependantTable, created, options);
            }
            block.Append(sb.CreateTableIfNotExists(table, options));
            created.Add(table);
            block.Append(sb.Go());
        }

        private static void MapParametersToColumns(IExpressionOperand operand, Dictionary<string, ContextColumn> mappingDictionary)
        {

            var bo = operand as BinaryOperand;
            if (bo != null)
            {
                var left = bo.Left as ValueOperand;

                if (left != null)
                {
                    var col = bo.Right as MemberExpressionOperand;
                    if (col != null)
                    {
                        mappingDictionary.Add(left.ParamName,
                            col.ColumnReference);
                    }

                }
                else
                {
                    MapParametersToColumns(bo.Left, mappingDictionary);
                }
                var right = bo.Right as ValueOperand;

                if (right != null)
                {
                    var col = bo.Left as MemberExpressionOperand;
                    if (col != null)
                    {
                        mappingDictionary.Add(right.ParamName,
                            col.ColumnReference);
                    }

                }
                else
                {
                    MapParametersToColumns(bo.Right, mappingDictionary);
                }
            }
        }


        #region Classes for fluent typing of conditional statements
        public static IfCondition If(this ScriptBuilder sb)
        {
            var block = new ScriptBlock();
            return new IfCondition(sb, block, "IF");
        }

        public class IfCondition
        {
            private readonly ScriptBuilder _scriptBuilder;
            private readonly ScriptBlock _block;
            private string _currentTextLine;

            internal IfCondition(ScriptBuilder scriptBuilder, ScriptBlock block, string currentTextLine)
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
                private readonly ScriptBuilder _scriptBuilder;
                private readonly ScriptBlock _block;
                private string _currentTextLine;

                public IfConditionToEvaluate(ScriptBuilder scriptBuilder, ScriptBlock block, string currentTextLine)
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
                    _currentTextLine += $"(SELECT top 1 * FROM sys.columns WHERE Name = {(_scriptBuilder.DbProvider.UseNationalCharacterSet ? "N" : "")}'{column}' AND Object_ID = Object_ID(N'{table}')";
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
                    _currentTextLine += $"(SELECT top 1 * FROM sys.objects WHERE object_id = OBJECT_ID({(_scriptBuilder.DbProvider.UseNationalCharacterSet ? "N" : "")}'{name}'))";
                    _block.Add(_currentTextLine);

                    return new ThenBlock(_scriptBuilder, _block);
                }
            }

            public class ThenBlock
            {
                private readonly ScriptBuilder _scriptBuilder;
                private readonly ScriptBlock _block;


                public ThenBlock(ScriptBuilder scriptBuilder, ScriptBlock block)
                {
                    _scriptBuilder = scriptBuilder;
                    _block = block;

                }

                public EndOrElse Then(Func<ScriptBuilder, ScriptBlock> func)
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
                private readonly ScriptBuilder _scriptBuilder;
                private readonly ScriptBlock _block;

                public EndOrElse(ScriptBuilder scriptBuilder, ScriptBlock block)
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

                public ScriptBlock Else(Func<ScriptBuilder, ScriptBlock> func)
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
