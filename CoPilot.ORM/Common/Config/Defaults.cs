using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Database.Commands.SqlWriters.Interfaces;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Common.Config
{
    public static class Defaults
    {
        public static void RegisterDefaults(ResourceLocator resourceLocator)
        {
            resourceLocator.Register<IFilterExpressionWriter, FilterExpressionWriter>();
            resourceLocator.Register<ICreateStatementWriter, SqlCreateStatementWriter>();
            resourceLocator.Register<IInsertStatementWriter, SqlInsertStatementWriter>();
            resourceLocator.Register<IUpdateStatementWriter, SqlUpdateStatementWriter>();
            resourceLocator.Register<IDeleteStatementWriter, SqlDeleteStatementWriter>();
            resourceLocator.Register<ISelectStatementWriter>(new SqlSelectStatementWriter(resourceLocator.Get<IFilterExpressionWriter>()));
            resourceLocator.Register<IModelValidator, SimpleModelValidator>();
        }
    }
}
