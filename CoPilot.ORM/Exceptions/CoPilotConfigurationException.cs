using System;

namespace CoPilot.ORM.Exceptions
{
    public class CoPilotConfigurationException : ArgumentException
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
