using System;
using CoPilot.ORM.Common;

namespace CoPilot.ORM.Logging
{
    public interface ILogger
    {
        LoggingLevel LoggingLevel { get; set; }

        void LogVerbose(string logText, string details = null);
        void LogInfo(string logText, string details = null);
        void LogWarning(string logText, string details = null);
        void LogError(string logText, string details = null);
        void LogException(Exception exception);

    }

    
}
