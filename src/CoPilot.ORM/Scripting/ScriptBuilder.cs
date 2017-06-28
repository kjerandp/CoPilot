using System;
using System.Collections.Generic;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Config.Naming;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Scripting
{
    /// <summary>
    /// Used to generate SQL scripts
    /// </summary>
    public class ScriptBuilder 
    {
        public DbModel Model { get; }
        public IDbProvider DbProvider { get; }

        public ScriptBuilder(IDbProvider provider, DbModel model)
        {
            Model = model;
            DbProvider = provider;
        }

        public ScriptBlock CreateTable<T>(CreateOptions options = null) where T : class
        {
            var table = Model.GetTableMap(typeof(T)).Table;
            return DbProvider.CommonScriptingTasks.CreateTable(table, options);
        }
        public ScriptBlock CreateTable(DbTable table, CreateOptions options = null)
        {
            return DbProvider.CommonScriptingTasks.CreateTable(table, options);
        }

        public ScriptBlock InsertTable<T>(T obj, ScriptOptions options = null) where T : class
        {
            options = options ?? ScriptOptions.Default();

            return GetInsertStatement(obj, options).Script;
        }
        
        public ScriptBlock InsertTable(DbTable tableDefinition, object template, ScriptOptions options = null)
        {
            var map = new TableMapEntry(template.GetType(), tableDefinition, OperationType.Insert);
            var ctx = new TableContext(Model, map);
            var opCtx = ctx.InsertUsingTemplate(ctx, template);
            return DbProvider.InsertStatementWriter.GetStatement(opCtx, options).Script;
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
        
        private SqlStatement GetInsertStatement(object entity, ScriptOptions options = null)
        {
            options = options ?? ScriptOptions.Default();
            var ctx = new TableContext(Model, entity.GetType());
            var opCtx = ctx.Insert(ctx, entity);

            return DbProvider.InsertStatementWriter.GetStatement(opCtx, options);
        }
   
        public ScriptBlock UseDatabase(string databaseName)
        {
            return DbProvider.CommonScriptingTasks.UseDatabase(databaseName);
        }

        public ScriptBlock CreateDatabase(string databaseName)
        {
            return DbProvider.CommonScriptingTasks.CreateDatabase(databaseName);
        }

        public ScriptBlock DropDatabase(string databaseName, bool autoCloseConnections = true)
        {
            return DbProvider.CommonScriptingTasks.DropDatabase(databaseName, autoCloseConnections);
        }

        public ScriptBlock DropCreateDatabase(string databaseName)
        {
            return DbProvider.CommonScriptingTasks.DropCreateDatabase(databaseName);
        }

        public ScriptBlock CreateTablesIfNotExists(CreateOptions options = null)
        {
            var toCreate = Model.Tables.ToList();
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

        public ScriptBlock CreateTableIfNotExists<T>(CreateOptions options = null) where T : class
        {
            var createScript = CreateTable<T>(options);
            return createScript;
        }

        #region move to provider specific extension methods
        public ScriptBlock CreateStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body)
        {
            return DbProvider.CommonScriptingTasks.CreateStoredProcedure(name, parameters, body);
        }

        public ScriptBlock CreateOrReplaceStoredProcedure(string name, DbParameter[] parameters, ScriptBlock body)
        {
            return DbProvider.CommonScriptingTasks.CreateOrReplaceStoredProcedure(name, parameters, body);
        }

        public ScriptBlock DropStoredProcedure(string name)
        {
            return DbProvider.CommonScriptingTasks.DropStoredProcedure(name);
        }

        public ScriptBlock CreateStoredProcedureFromQuery<T>(string name, Expression<Func<T, bool>> filter = null, ISingleStatementQueryWriter scriptCreator = null, params string[] include) where T : class
        {
            var ctx = Model.CreateContext<T>(include);

            if (filter != null)
            {
                var decoder = new ExpressionDecoder(DbProvider);
                var expression = decoder.Decode(filter.Body);
                ctx.ApplyFilter(expression);
            }

            return CreateStoredProcedureFromQuery(name, ctx, scriptCreator);
        }

        public ScriptBlock CreateStoredProcedureFromQuery(string name, TableContext ctx, ISingleStatementQueryWriter scriptCreator = null)
        {
            if (scriptCreator == null)
                scriptCreator = DbProvider.SingleStatementQueryWriter;

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
                var newName = DbProvider.GetStoredProcedureParameterName(
                    caseConverter.Convert(contextColumn.Node.MapEntry.GetMappedMember(contextColumn.Column).Name)
                );

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
            var scriptBlock = MultiLineComment(comment);
            scriptBlock.AddMultiLineText(script, false);
            return DbProvider.CommonScriptingTasks.CreateOrReplaceStoredProcedure(name, parameters.ToArray(), scriptBlock);
        }
        #endregion

        private void CreateTableAndDependantTables(ScriptBlock block, DbTable table, List<DbTable> created, CreateOptions options)
        {

            var dependantTables = table.Columns.Where(r => r.IsForeignKey).Select(r => r.ForeignkeyRelationship.PrimaryKeyColumn.Table).Distinct();
            foreach (var dependantTable in dependantTables.Where(r => !created.Contains(r)))
            {
                CreateTableAndDependantTables(block, dependantTable, created, options);
            }
            block.Append(DbProvider.CommonScriptingTasks.CreateTableIfNotExists(table, options));
            created.Add(table);
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


    }
}
