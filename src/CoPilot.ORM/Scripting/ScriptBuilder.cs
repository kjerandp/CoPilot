using System;
using CoPilot.ORM.Common;
using CoPilot.ORM.Context;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Options;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Mapping;
using CoPilot.ORM.Model;

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
        
        private SqlStatement GetCreateStatement(Type entityType, CreateOptions options = null)
        {
            var table = Model.GetTableMap(entityType).Table;
            return GetCreateStatement(table, options);
        }

        private SqlStatement GetCreateStatement(DbTable table, CreateOptions options)
        {
            options = options ?? CreateOptions.Default();
            
            return DbProvider.CreateStatementWriter.GetStatement(table, options);
        }

    
    }
}
