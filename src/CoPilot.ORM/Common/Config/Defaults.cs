using CoPilot.ORM.Database.Commands.Query;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Common.Config
{
    public static class Defaults
    {
        public static void RegisterDefaults(ResourceLocator resourceLocator)
        {
            resourceLocator.Register<ICreateStatementWriter, SqlCreateStatementWriter>();
            resourceLocator.Register<IInsertStatementWriter, SqlInsertStatementWriter>();
            resourceLocator.Register<IUpdateStatementWriter, SqlUpdateStatementWriter>();
            resourceLocator.Register<IDeleteStatementWriter, SqlDeleteStatementWriter>();
            resourceLocator.Register<ISelectStatementWriter>(new SqlSelectStatementWriter());
            resourceLocator.Register<IModelValidator, SimpleModelValidator>();
            resourceLocator.Register<IQueryBuilder, SqlQueryBuilder>();
            resourceLocator.Register<ICommonScriptingTasks, SqlCommonScriptingTasks>();
            resourceLocator.Register<IQueryStrategySelector>(new SqlQueryStrategySelector(
                resourceLocator.Get<IQueryBuilder>(),
                resourceLocator.Get<ISelectStatementWriter>()
            ));

        }
    }
}
