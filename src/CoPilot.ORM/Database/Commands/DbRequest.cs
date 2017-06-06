using System.Collections.Generic;
using System.Data;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database.Commands
{
    public abstract class DbRequest
    {
        public IDbCommand Command { get; set; }

        public List<DbParameter> Parameters { get; set; } = new List<DbParameter>();

        public Dictionary<string, object> Args { get; set; }
        
        public abstract CommandType CommandType { get; }

        public void AddArgument(string name, object value)
        {
            if (Args == null)
            {
                Args = new Dictionary<string, object>();
            }
            if (Args.ContainsKey(name))
            {
                Args[name] = value;
            }
            else
            {
                Args.Add(name, value);
            }
        }

        public abstract void SetArguments(object args);
        

        internal static DbRequest CreateRequest(string commandText, object args)
        {
            //TODO: check if args is a mapped entity - if so apply any adaptors mapped to its properties
            if (commandText.Split(' ', '\n').Length > 1)
            {
                var stm = new SqlStatement();
                stm.Script.Add(commandText);
                if (args != null) stm.SetArguments(args);
                return stm;
            }

            var p = new SqlStoredProcedure(commandText);
            if (args != null) p.SetArguments(args);
            return p;

        }

        internal static DbRequest CreateRequest(DbModel model, string commandText, object args)
        {
            if (commandText.Split(' ', '\n').Length == 1)
            {
                var proc = model.GetStoredProcedure(commandText);
                if (proc != null)
                {
                    return SqlStoredProcedure.CreateRequest(proc, args);
                }
            }
            return CreateRequest(commandText, args);
        }

    }
}