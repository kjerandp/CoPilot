using System;

namespace CoPilot.ORM.Logging
{
    public interface ILogger
    {
        void LogVerbose(string logText, string details = null);
        void LogInfo(string logText, string details = null);
        void LogWarning(string logText, string details = null);
        void LogError(string logText, string details = null);
        void LogException(Exception exception);

        bool SuppressLogging { get; set; }
    }

    
}
