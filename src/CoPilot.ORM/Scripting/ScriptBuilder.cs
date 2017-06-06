using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Config.Naming;
using CoPilot.ORM.Context;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.Query.Strategies;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Scripting
{
    /// <summary>
    /// Used to generate SQL scripts
    /// </summary>
    public class ScriptBuilder
    {
        private readonly DbModel _model;
        private readonly IDbProvider _provider;

        public ScriptBuilder(IDbProvider provider, DbModel model)
        {
            _model = model;
            _provider = provider;
        }

        public ScriptBlock CreateTable<T>(CreateOptions options = null) where T : class
        {
            return GetCreateStatement(typeof(T), options).Script;
        }

        public ScriptBlock CreateTable(DbTable table, CreateOptions options)
        {
            options = options ?? CreateOptions.Default();
            return GetCreateStatement(table, options).Script;
        }

        public ScriptBlock InsertTable<T>(T obj, ScriptOptions options = null) where T : class
        {
            options = options ?? ScriptOptions.Default();

            return GetInsertStatement(obj, options).Script;
        }

        public ScriptBlock CreateTableIfNotExists<T>(CreateOptions options = null) where T : class
        {
            var table = _model.GetTableMap<T>().Table;

            var createScript = CreateTable<T>(options);
            var block = If().NotExists().Table(table.TableName).Then(createScript).End();
            return block;
        }

        public ScriptBlock CreateOrReplaceStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body)
        {
            var script = If().Exists().StoredProcedure(name).Then(DropStoredProcedure(name)).End();
            script.Append(Go());
            script.Append(CreateStoredProcedure(name, parameters, body));
            return script;
        }

        public ScriptBlock CreateStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body)
        {
            if (string.IsNullOrEmpty(name)) throw new CoPilotUnsupportedException("You need to provide a name for the stored procedure");

            var paramsString = string.Join(", ",
                parameters.Select(GetParameterAsString));

            if (!string.IsNullOrEmpty(paramsString))
            {
                paramsString = $"({paramsString})";
            }

            var script = new ScriptBlock($"CREATE PROCEDURE {name} {paramsString}","AS","BEGIN");
            script.AddMultiLineText(body.ToString());
            script.AddMultiLineText("END", false);
            return script;
        }

        public string GetParameterAsString(DbParameter prm)
        {
            var str = prm.Name + " " + _provider.GetDataTypeAsString(prm.DataType, prm.Size);
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


        public ScriptBlock CreateStoredProcedureFromQuery<T>(string name, Expression<Func<T, bool>> filter = null, IQueryScriptCreator scriptCreator = null, params string[] include) where T : class
        {
            var ctx = _model.CreateContext<T>(include);

            if (filter != null)
            {
                var expression = ExpressionHelper.DecodeExpression(filter);
                ctx.ApplyFilter(expression);
            }

            return CreateStoredProcedureFromQuery(name, ctx, scriptCreator);
        }

        public ScriptBlock CreateStoredProcedureFromQuery(string name, TableContext ctx, IQueryScriptCreator scriptCreator = null) 
        {
            if(scriptCreator == null)
                scriptCreator = new TempTableJoinStrategy(_provider.QueryBuilder, _provider.SelectStatementWriter);

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
                    if (_provider.DataTypeHasSize(contextColumn.Column.DataType))
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
            var scriptBlock = MultiLineComment(comment);
            scriptBlock.AddMultiLineText(script, false);
            return CreateOrReplaceStoredProcedure(name, parameters.ToArray(), scriptBlock);
        }

        private static void MapParametersToColumns(IExpressionOperand operand, Dictionary<string, ContextColumn> mappingDictionary)
        {
            
            var bo = operand as BinaryOperand;
            if (bo != null)
            {
                var left = bo.Left as ValueOperand;

                if (left != null)
                {
                    var col = bo.Right as ContextMemberOperand;
                    if (col != null)
                    {
                        mappingDictionary.Add(left.ParamName,
                            col.ContextColumn);
                    }

                }
                else
                {
                    MapParametersToColumns(bo.Left, mappingDictionary);
                }
                var right = bo.Right as ValueOperand;

                if (right != null)
                {
                    var col = bo.Left as ContextMemberOperand;
                    if (col != null)
                    {
                        mappingDictionary.Add(right.ParamName,
                            col.ContextColumn);
                    }

                }
                else
                {
                    MapParametersToColumns(bo.Right, mappingDictionary);
                }
            }
        }

        public ScriptBlock CreateTableIfNotExists(DbTable table, CreateOptions options = null)
        {
            var createScript = CreateTable(table, options);
            var block = If().NotExists().Table(table.TableName).Then(createScript).End();
            return block;
        }

        public ScriptBlock InsertTable(DbTable tableDefinition, object template, ScriptOptions options = null)
        {
            var map = new TableMapEntry(template.GetType(), tableDefinition, OperationType.Insert);
            var ctx = new TableContext(_model, map);
            var opCtx = ctx.InsertUsingTemplate(ctx, template);
            return _provider.InsertStatementWriter.GetStatement(opCtx, options).Script;
        }
        
        public ScriptBlock InsertIntoTableIfEmpty<T>(ScriptOptions options = null, params T[] entities) where T : class
        {
            options = options ?? ScriptOptions.Default();

            var table = _model.GetTableMap<T>().Table;
            var insertBlock = new ScriptBlock();

            foreach (var entity in entities)
            {
                insertBlock.Append(InsertTable(entity, options));
            }
            if (options.EnableIdentityInsert)
            {
                _provider.CommonScriptingTasks.WrapInsideIdentityInsertScript(table.ToString(), insertBlock);
            }
            
            var block = If().NotExists().TableData(table.TableName).Then(insertBlock).End();
            return block;
        }

        public ScriptBlock InsertIntoTableIfEmpty<T>(T obj, ScriptOptions options = null, object additionalValues = null) where T : class
        {
            options = options ?? ScriptOptions.Default();

            var insertBlock = new ScriptBlock();
            var table = _model.GetTableMap<T>().Table;
            
            insertBlock.Append(InsertTable(obj, options));
            if (options.EnableIdentityInsert)
            {
                _provider.CommonScriptingTasks.WrapInsideIdentityInsertScript(table.ToString(), insertBlock);
            }
            var block = If().NotExists().TableData(table.TableName).Then(insertBlock).End();
            return block;
        }

        public ScriptBlock InsertIntoTableIfEmpty(DbTable tableDefinition, ScriptOptions options = null, params object[] templateObjects) 
        {
            options = options ?? ScriptOptions.Default();

            var insertBlock = new ScriptBlock();
            foreach (var entity in templateObjects)
            {
                insertBlock.Append(InsertTable(tableDefinition, entity, options));
            }
            if (options.EnableIdentityInsert)
            {
                _provider.CommonScriptingTasks.WrapInsideIdentityInsertScript(tableDefinition.ToString(), insertBlock);
            }
            var block = If().NotExists().TableData(tableDefinition.TableName).Then(insertBlock).End();
            return block;
        }

        public ScriptBlock UseDatabase(string databaseName)
        {
            var block = new ScriptBlock();

            block.Add($"USE {databaseName}");

            return block;
        }

        public ScriptBlock DropCreateDatabase(string databaseName)
        {
            var block = UseDatabase("master");

            block.Append(Go());

            block.Append(If().Exists().Database(databaseName).Then(r => r.DropDatabase(databaseName)).End());

            block.Add();
            block.Append(CreateDatabase(databaseName));

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
                block.Add($"WHERE DBId = DB_ID(N'{databaseName}') AND SPId <> @@SPId");
                block.Add("EXEC(@SQL)");
            }
            block.Add($"DROP DATABASE {databaseName}");

            return block;
        }

        public ScriptBlock DropStoredProcedure(string name)
        {
            return new ScriptBlock($"DROP PROCEDURE {name}");
        }

        public ScriptBlock Go(int times = 0)
        {
            var block = new ScriptBlock();
            block.Add("GO" + (times > 0 ? " " + times : ""), "");
            return block;
        }

        public ScriptBlock SingleLineComment(string comment)
        {
            var block = new ScriptBlock();
            block.Add("--" + comment.Replace('\n', ' '));
            return block;
        }

        public ScriptBlock MultiLineComment(string comment)
        {

            var commentLines = comment.Split('\n');

            var block = new ScriptBlock();
            block.Add("/*");
            block.AddAsNewBlock(commentLines);
            block.Add("*/");
            return block;
        }

        public ScriptBlock CreateTablesIfNotExists(CreateOptions options = null)
        {
            var toCreate = _model.Tables.ToList();
            var created = new List<DbTable>();
            const int max = 1000;
            var i = 0;
            var block = new ScriptBlock();


            while (!toCreate.All(r => created.Contains(r)) && i < max)
            {
                var table = toCreate.First(r => !created.Contains(r));
                CreateTableAndDependantTables(block, table, created, options);
                i++;
            }
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
                    _currentTextLine += $"(SELECT top 1 * FROM sys.columns WHERE Name = N'{column}' AND Object_ID = Object_ID(N'{table}')";
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
                    _currentTextLine += $"(SELECT top 1 * FROM sys.objects WHERE object_id = OBJECT_ID(N'{name}'))";
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
        
        private void CreateTableAndDependantTables(ScriptBlock block, DbTable table, List<DbTable> created, CreateOptions options)
        {

            var dependantTables = table.Columns.Where(r => r.IsForeignKey).Select(r => r.ForeignkeyRelationship.PrimaryKeyColumn.Table).Distinct();
            foreach (var dependantTable in dependantTables.Where(r => !created.Contains(r)))
            {
                CreateTableAndDependantTables(block, dependantTable, created, options);
            }
            block.Append(CreateTableIfNotExists(table, options));
            created.Add(table);
            block.Append(Go());
        }

        private SqlStatement GetInsertStatement(object entity, ScriptOptions options = null)
        {
            options = options ?? ScriptOptions.Default();
            var ctx = new TableContext(_model, entity.GetType());
            var opCtx = ctx.Insert(ctx, entity);

            return _provider.InsertStatementWriter.GetStatement(opCtx, options);
        }
        
        private SqlStatement GetCreateStatement(Type entityType, CreateOptions options = null)
        {
            var table = _model.GetTableMap(entityType).Table;
            return GetCreateStatement(table, options);
        }

        private SqlStatement GetCreateStatement(DbTable table, CreateOptions options)
        {
            options = options ?? CreateOptions.Default();
            
            return _provider.CreateStatementWriter.GetStatement(table, options);
        }

    
    }
}
