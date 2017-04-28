using CoPilot.ORM.Common.Config;
using CoPilot.ORM.Logging;

namespace CoPilot.ORM.Common
{
    public static class CoPilotGlobalResources 
    {
        static CoPilotGlobalResources()
        {
            Locator.Register<ILogger, ConsoleLogger>();
        }
        public static ResourceLocator Locator = new ResourceLocator();
        public static OperationType DefaultOperations = OperationType.Select | OperationType.Update | OperationType.Insert;

        public static LoggingLevel LoggingLevel
        {
            get { return Locator.Get<ILogger>().LoggingLevel; }
            set { Locator.Get<ILogger>().LoggingLevel = value; }
        }

    }
}
