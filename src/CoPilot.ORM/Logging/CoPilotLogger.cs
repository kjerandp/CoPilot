using System;
using CoPilot.ORM.Common;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Logging
{
    public class CoPilotLogger : ILogger
    {
        private readonly IDbProvider _provider;

        public CoPilotLogger(IDbProvider provider)
        {
            _provider = provider;
            Output = new ConsoleLogWriter();
            
        }

        public CoPilotLogger(IDbProvider provider, ILogOutputWriter outputWriter)
        {
            _provider = provider;
            Output = outputWriter;
        }

        public ILogOutputWriter Output { get; }

        public void LogVerbose(string logText, string details = null)
        {
            if ((int)_provider.LoggingLevel < (int)LoggingLevel.Verbose || SuppressLogging) return;
            var block = new ScriptBlock();
            
            block.Add("[VERBOSE]");
            block.Add(logText);
            if (details != null)
            {
                block.Add("Details:");
                block.Add(new ScriptBlock(details.Split('\n')));
            }

            Output.WriteLine(block);
            Output.WriteLine();
            
        }

        public void LogInfo(string logText, string details = null)
        {
            if ((int)_provider.LoggingLevel < (int)LoggingLevel.Info || SuppressLogging) return;
            var block = new ScriptBlock();
            
            block.Add("[INFO]");
            block.Add(logText);
            if (details != null)
            {
                block.Add("Details:");
                block.Add(new ScriptBlock(details.Split('\n')));
            }

            Output.WriteLine(block);
            Output.WriteLine();
            
        }

        public void LogWarning(string logText, string details = null)
        {
            if ((int)_provider.LoggingLevel < (int)LoggingLevel.Warning || SuppressLogging) return;
            var block = new ScriptBlock();
            
            block.Add("[WARNNG]");
            block.Add(logText);
            if (details != null)
            {
                block.Add("Details:");
                block.Add(new ScriptBlock(details.Split('\n')));
            }
            Output.WriteLine(block);
            Output.WriteLine();
            

        }

        public void LogError(string logText, string details = null)
        {
            if ((int) _provider.LoggingLevel < (int) LoggingLevel.Error || SuppressLogging) return;

            var block = new ScriptBlock();
            block.Add("[ERROR]");
            block.Add(logText);
            if (details != null)
            {
                block.Add("Details:");
                block.Add(new ScriptBlock(details.Split('\n')));
            }

            Output.WriteLine(block);
            Output.WriteLine();
        }

        public void LogException(Exception exception)
        {
            LogError(exception.GetType().Name, exception.Message+": "+exception.StackTrace);
        }

        public bool SuppressLogging { get; set; }
    }

    public class ConsoleLogger : CoPilotLogger
    {
        public ConsoleLogger(IDbProvider provider):base(provider, new ConsoleLogWriter()) { }
    }
}