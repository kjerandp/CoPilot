using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Exceptions;
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
        public static void ReplaceArgsInCommand(this SqlCommand command, Dictionary<string, object> args)
        {
            if(command.Parameters == null || command.Parameters.Count == 0) throw new CoPilotUnsupportedException("Command doesn't have any parameters defined!");

            for (var i=0; i<command.Parameters.Count;i++)
            {
                var param = command.Parameters[i];
                if (param.Direction == ParameterDirection.Output) continue;
                
                if (!args.ContainsKey(param.ParameterName)) param.Value = DBNull.Value;

                var enumerable = args[param.ParameterName] as ICollection<object>;
                if (enumerable != null)
                {
                    throw new CoPilotUnsupportedException("Collections are not supported for this operation!");
                }
                
                 param.Value = args[param.ParameterName] ?? DBNull.Value;

            }
        }

    }
}
