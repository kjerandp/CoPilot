﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CoPilot.ORM.Helpers;
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

        public override void SetArguments(object args)
        {
            var stm = Script.ToString();

            var props = args.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var name = "@" + prop.Name;
                var value = prop.GetValue(args, null);
                if (value == null || stm.IndexOf(name, StringComparison.Ordinal) < 0) continue;

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
