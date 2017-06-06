
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Common.Config
{
    public static class Defaults
    {
        public static void RegisterDefaults(ResourceLocator resourceLocator)
        {  
            resourceLocator.Register<IModelValidator, SimpleModelValidator>();
        }
    }
}
