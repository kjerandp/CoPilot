using CoPilot.ORM.Database.Commands.Options;
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
            var commonScripting = (SqlCommonScriptingTasks)sb.DbProvider.CommonScriptingTasks;


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
            

            var block = commonScripting.If().NotExists().TableData(table.TableName).Then(insertBlock).End();
            return block;
        }

        public static ScriptBlock InsertIntoTableIfEmpty<T>(this ScriptBuilder sb, T obj, ScriptOptions options = null, object additionalValues = null) where T : class
        {
            var commonScripting = (SqlCommonScriptingTasks)sb.DbProvider.CommonScriptingTasks;

            options = options ?? ScriptOptions.Default();

            var insertBlock = new ScriptBlock();
            var table = sb.Model.GetTableMap<T>().Table;

            insertBlock.Append(sb.InsertTable(obj, options));
            if (options.EnableIdentityInsert)
            {
                sb.DbProvider.CommonScriptingTasks.WrapInsideIdentityInsertScript(table, insertBlock);
            }
            var block = commonScripting.If().NotExists().TableData(table.TableName).Then(insertBlock).End();
            return block;
        }

        public static ScriptBlock InsertIntoTableIfEmpty(this ScriptBuilder sb, DbTable tableDefinition, ScriptOptions options = null, params object[] templateObjects)
        {
            var commonScripting = (SqlCommonScriptingTasks)sb.DbProvider.CommonScriptingTasks;

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
            var block = commonScripting.If().NotExists().TableData(tableDefinition.TableName).Then(insertBlock).End();
            return block;
        }
        
        
    }
}
