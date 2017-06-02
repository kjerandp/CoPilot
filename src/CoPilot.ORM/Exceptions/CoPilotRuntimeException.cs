using System;

namespace CoPilot.ORM.Exceptions
{
    public class CoPilotRuntimeException : CoPilotException
    {
        public CoPilotRuntimeException() { }
        public CoPilotRuntimeException(string message) : base(message)
        {
        }

        public CoPilotRuntimeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}