using System;
using CoPilot.ORM.Common;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Logging
{
    public class ConsoleLogger : ILogger
    {
        public LoggingLevel LoggingLevel { get; set; }

        public void LogVerbose(string logText, string details = null)
        {
            var block = new ScriptBlock();
            if ((int)LoggingLevel >= (int)LoggingLevel.Verbose)
            {
                block.Add("[VERBOSE]");
                block.Add(logText);
                if (details != null)
                {
                    block.Add("Details:");
                    block.Add(new ScriptBlock(details.Split('\n')));
                }
                
                Console.WriteLine(block.ToString());
                Console.WriteLine();
            }
        }

        public void LogInfo(string logText, string details = null)
        {
            var block = new ScriptBlock();
            if ((int)LoggingLevel >= (int)LoggingLevel.Info)
            {
                block.Add("[INFO]");
                block.Add(logText);
                if (details != null)
                {
                    block.Add("Details:");
                    block.Add(new ScriptBlock(details.Split('\n')));
                }
              
                Console.WriteLine(block.ToString());
                Console.WriteLine();
            }
        }

        public void LogWarning(string logText, string details = null)
        {
            var block = new ScriptBlock();
            if ((int)LoggingLevel >= (int)LoggingLevel.Warning)
            {
                block.Add("[WARNNG]");
                block.Add(logText);
                if (details != null)
                {
                    block.Add("Details:");
                    block.Add(new ScriptBlock(details.Split('\n')));
                }
                Console.WriteLine(block.ToString());
                Console.WriteLine();
            }

        }

        public void LogError(string logText, string details = null)
        {
            var block = new ScriptBlock();
            if ((int) LoggingLevel >= (int) LoggingLevel.Error)
            {
                block.Add("[ERROR]");
                block.Add(logText);
                if (details != null)
                {
                    block.Add("Details:");
                    block.Add(new ScriptBlock(details.Split('\n')));
                }
               
                Console.WriteLine(block.ToString());
                Console.WriteLine();
            }
            
        }

        public void LogException(Exception exception)
        {
            LogError(exception.GetType().Name, exception.Message+": "+exception.StackTrace);
        }
    }
}
