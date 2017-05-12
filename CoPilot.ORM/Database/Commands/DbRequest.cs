using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Model;

namespace CoPilot.ORM.Database.Commands
{
    public abstract class DbRequest
    {
        internal List<DbParameter> Parameters { get; set; }
        internal Dictionary<string, object> Args { get; set; }
        public abstract CommandType CommandType { get; }

        public void SetParameters(object args)
        {
            var props = args.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var name = "@" + prop.Name;
                var value = prop.GetValue(args, null);
                Parameters.Add(new DbParameter(name, DbConversionHelper.MapToDbDataType(value.GetType())));
                Args.Add(name, value);
            }
        }

        public void SetParameters(List<DbParameter> parameters, object args)
        {
            var props = args.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var prm = parameters.LeftJoin(props, p => p.Name.ToLower(), a => "@"+a.Name.ToLower(),
                (p, a) => new
                {
                    Parameter = p,
                    Arg = a != null ? a.GetValue(args, null) : p.DefaultValue
                }).ToArray();

            Parameters.AddRange(prm.Select(r => r.Parameter));
            Args = prm.ToDictionary(k => k.Parameter.Name, v => v.Arg);
        }

        internal static DbRequest CreateRequest(string commandText, object args)
        {
            //TODO: check if args is a mapped entity - if so apply any adaptors mapped to its properties
            if (commandText.Split(' ', '\n').Length > 1)
            {
                var stm = new SqlStatement();
                stm.Script.Add(commandText);
                if (args != null) stm.SetParameters(args);
                return stm;
            }

            var p = new SqlStoredProcedure(commandText);
            if (args != null) p.SetParameters(args);
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