using System;

namespace CoPilot.ORM.Exceptions
{
    public class CoPilotUnsupportedException : CoPilotException
    {
        public CoPilotUnsupportedException() { }
        public CoPilotUnsupportedException(string message) : base(message)
        {
        }

        public CoPilotUnsupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}