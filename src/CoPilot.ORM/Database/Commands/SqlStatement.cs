using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Scripting;
using System.Linq;

namespace CoPilot.ORM.Database.Commands
{
    public class SqlStatement : DbRequest
    {
        public SqlStatement():this(new ScriptBlock()){}

        public SqlStatement(ScriptBlock script)
        {
            Script = script;
        }
        public ScriptBlock Script { get; internal set; }
        public override CommandType CommandType => CommandType.Text;

        public override void SetArguments(object args)
        {
            Args = new Dictionary<string, object>();

            var stm = Script.ToString();

            var props = args.GetType().GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var prop in props)
            {
                var name = "@" + prop.Name;
                var value = prop.GetValue(args, null);
                if (value == null || stm.IndexOf(name, StringComparison.Ordinal) < 0) continue;
                if(!Parameters.Any(r => r.Name.Equals(name)))
                    Parameters.Add(new DbParameter(name, DbConversionHelper.MapToDbDataType(value.GetType())));
                Args.Add(name, value);
            }
            
        }

        public override string ToString()
        {
            return Script.ToString();
        }
    }
}
