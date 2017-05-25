using System;
using System.Data;

namespace CoPilot.ORM.Exceptions
{
    public class CoPilotDataException : Exception
    {
        public CoPilotDataException() { }
        public CoPilotDataException(string message) : base(message)
        {
        }

        public CoPilotDataException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}