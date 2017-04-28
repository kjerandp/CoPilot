using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Extensions
{
    public static class AdoNetExtensions
    {
        public static void AddArrayParameters<T>(this SqlCommand cmd, string name, IEnumerable<T> values)
            where T : class
        {
            name = name.StartsWith("@") ? name : "@" + name;
            var names = string.Join(", ", values.Select((value, i) =>
            {
                var paramName = name + i;
                cmd.Parameters.AddWithValue(paramName, value);
                return paramName;
            }));
            cmd.CommandText = cmd.CommandText.Replace(name, names);
        }

        
        public static void AddArgsToCommand(this SqlCommand command, List<DbParameter> parameters, Dictionary<string, object> args)
        {
            if(command.Parameters.Count > 0) command.Parameters.Clear();

            if (parameters == null) return;
            foreach (var param in parameters)
            {
                if (param.IsOutput)
                {
                    command.Parameters.Add(param.Name, DbConversionHelper.ToDbType(param.DataType), param.Size).Direction = ParameterDirection.Output;
                }
                else
                {
                    if (!args.ContainsKey(param.Name)) continue;

                    var enumerable = args[param.Name] as ICollection<object>;
                    if (enumerable != null)
                    {
                        command.AddArrayParameters(param.Name, enumerable);
                    }
                    else
                    {
                        command.Parameters.AddWithValue(param.Name, args[param.Name] ?? DBNull.Value);
                    }
                }

            }
        }


    }
}
