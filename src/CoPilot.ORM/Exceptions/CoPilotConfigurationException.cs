using System;

namespace CoPilot.ORM.Exceptions
{
    public class CoPilotConfigurationException : CoPilotException
    {
        public CoPilotConfigurationException() { }
        public CoPilotConfigurationException(string message) : base(message)
        {
        }

        public CoPilotConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
