using CoPilot.ORM.Model;

namespace CoPilot.ORM.Config.Builders
{
    public abstract class BaseBuilder
    {
        protected DbModel Model;

        protected BaseBuilder(DbModel model)
        {
            Model = model;
        }
    }
}
