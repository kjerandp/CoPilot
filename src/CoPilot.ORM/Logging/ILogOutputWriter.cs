using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Logging
{
    public interface ILogOutputWriter
    {
        void WriteLine(ScriptBlock block = null);
    }
}
