using System;

namespace CoPilot.ORM.Exceptions
{
    public abstract class CoPilotException : Exception
    {
        protected CoPilotException() { }

        protected CoPilotException(string message) : base(message)
        {
        }

        protected CoPilotException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}