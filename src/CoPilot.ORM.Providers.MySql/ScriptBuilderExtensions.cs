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
using CoPilot.ORM.Providers.MySql.Writers;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Providers.MySql
{
    public static class ScriptBuilderExtensions
    {
        public static ScriptBlock UseDatabase(this ScriptBuilder sb, string databaseName)
        {
            var block = new ScriptBlock();

            block.Add($"USE {databaseName.ToLower()};");

            return block;
        }

        public static ScriptBlock DropCreateDatabase(this ScriptBuilder sb, string databaseName)
        {
            var block = sb.UseDatabase("sys");
            
            block.Append(sb.DropDatabase(databaseName));
            block.Append(sb.CreateDatabase(databaseName));

            return block;
        }

        public static ScriptBlock CreateDatabase(this ScriptBuilder sb, string databaseName)
        {
            var block = new ScriptBlock();

            block.Add($"CREATE DATABASE IF NOT EXISTS {databaseName.ToLower()};");

            return block;
        }

        public static ScriptBlock DropDatabase(this ScriptBuilder sb, string databaseName, bool autoCloseConnections = true)
        {
            var block = new ScriptBlock();
            
            block.Add($"DROP DATABASE IF EXISTS {databaseName.ToLower()};");

            return block;
        }

        public static ScriptBlock DropStoredProcedure(this ScriptBuilder sb, string name)
        {
            return new ScriptBlock($"DROP PROCEDURE IF EXISTS {name};");
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
            var createScript = sb.CreateTable<T>(options);
            return createScript;
        }

        public static ScriptBlock CreateOrReplaceStoredProcedure(this ScriptBuilder sb, string name, DbParameter[] parameters, ScriptBlock body)
        {
            var script = sb.DropStoredProcedure(name);
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
                var newName = caseConverter.Convert(
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
            return sb.CreateTable(table, options);
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

            var script = new ScriptBlock($"CREATE PROCEDURE {name} {paramsString}","BEGIN");
            script.AddMultiLineText(body.ToString());
            script.AddMultiLineText("END;", false);
            return script;
        }

        private static string GetParameterAsString(this ScriptBuilder sb, DbParameter prm)
        {
            var dataTypeText = sb.DbProvider.GetDataTypeAsString(prm.DataType, prm.Size);
            if (prm.NumberPrecision != null && dataTypeText.EndsWith("<precision>"))
            {
                dataTypeText = dataTypeText.Replace("<precision>",$"({prm.NumberPrecision.Scale},{prm.NumberPrecision.Precision})");
            }
            var str = prm.Name + " " + dataTypeText;
            
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
