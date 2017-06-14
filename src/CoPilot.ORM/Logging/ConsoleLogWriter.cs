using System;
using CoPilot.ORM.Common;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Logging
{
    public class ConsoleLogWriter : ILogOutputWriter
    {
        public LoggingLevel LoggingLevel { get; set; }
  
        public void WriteLine(ScriptBlock block = null)
        {
            Console.WriteLine(block?.ToString());
        }
    }
}
