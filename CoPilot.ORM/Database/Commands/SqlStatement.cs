using System.Collections.Generic;
using System.Data;
using CoPilot.ORM.Scripting;

namespace CoPilot.ORM.Database.Commands
{
    public class SqlStatement : DbRequest
    {
        public SqlStatement()
        {
            Parameters = new List<DbParameter>();
            Args = new Dictionary<string, object>();
            Script = new ScriptBlock();
        }
        public ScriptBlock Script { get; internal set; }
        public override CommandType CommandType => CommandType.Text;

        public override string ToString()
        {
            return Script.ToString();
        }
    }
}
