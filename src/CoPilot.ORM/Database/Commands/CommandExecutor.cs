using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Logging;

namespace CoPilot.ORM.Database.Commands
{
    public static class CommandExecutor
    {
        private static readonly object LockObj = new object();

        public static DbResponse ExecuteQuery(SqlConnection connection, DbRequest cmd, params string[] names)
        {
            return ExecuteQuery(new SqlCommand("", connection), cmd, names);
            
        }

        public static DbResponse ExecuteQuery(SqlCommand command, DbRequest cmd, params string[] names)
        {
            var logger = CoPilotGlobalResources.Locator.Get<ILogger>();
            var resultSets = new List<DbRecordSet>();
            var timer = Stopwatch.StartNew();

            try
            {
                lock (LockObj)
                {
                    if (command.Connection.State != ConnectionState.Open)
                        command.Connection.Open();

                    command.CommandText = cmd.ToString();
                    command.CommandType = cmd.CommandType;
                    command.AddArgsToCommand(cmd.Parameters, cmd.Args);

                    logger.LogVerbose("Executing Query", command.CommandText);

                    var rsIdx = 0;
                    using (var reader = command.ExecuteReader())
                    {
                        var hasResult = true;
                        while (hasResult)
                        {
                            var set = new DbRecordSet
                            {
                                Name = names != null && rsIdx < names.Length ? names[rsIdx] : null
                            };
                            if (!reader.HasRows)
                            {
                                set.FieldNames = new string[0];
                                set.FieldTypes = new Type[0];
                                set.Records = new object[0][];
                            }
                            else
                            {
                                var fieldNames = new string[reader.FieldCount];
                                var fieldTypes = new Type[reader.FieldCount];

                                for (var i = 0; i < reader.FieldCount; i++)
                                {
                                    fieldNames[i] = reader.GetName(i);
                                    fieldTypes[i] = reader.GetFieldType(i);
                                }
                                var valueset = new List<object[]>();
                                while (reader.Read())
                                {
                                    var values = new object[fieldNames.Length];
                                    for (var i = 0; i < fieldNames.Length; i++)
                                    {
                                        values[i] = reader.GetValue(i);
                                    }
                                    valueset.Add(values);
                                }
                                set.FieldNames = fieldNames;
                                set.FieldTypes = fieldTypes;
                                set.Records = valueset.ToArray();
                            }
                            resultSets.Add(set);
                            hasResult = reader.NextResult();
                            rsIdx++;
                        }

                        //reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CoPilotDataException("Unable to execute command!", ex);
            }

            var time = timer.ElapsedMilliseconds;
            logger.LogVerbose($"^ Executed in {time}ms (selected {resultSets.Sum(r => r.Records.Length)} rows)");
            return new DbResponse(resultSets.ToArray(), time);

        }
        
        public static int ExecuteNonQuery(SqlConnection connection, DbRequest cmd)
        {
            return ExecuteNonQuery(new SqlCommand("", connection), cmd);
        }

        public static int ExecuteNonQuery(SqlCommand command, DbRequest cmd)
        {
            var logger = CoPilotGlobalResources.Locator.Get<ILogger>();
            int result;

            try
            {
                lock (LockObj)
                {
                    var timer = Stopwatch.StartNew();
                    PrepareNonQuery(command, cmd);
                    logger.LogVerbose("Executing Non Query", command.CommandText);
                    result = command.ExecuteNonQuery();
                    var time = timer.ElapsedMilliseconds;
                    logger.LogVerbose($"^ Affected {result} rows in {time}ms");
                }
            }
            catch (Exception ex)
            {
                throw new CoPilotDataException("Unable to execute command!", ex);
            }
            
            return result;

        }

        public static void PrepareNonQuery(SqlCommand command, DbRequest cmd)
        {
            command.CommandText = string.Join(";\n", SplitSqlStatements(cmd.ToString()));
            command.CommandType = cmd.CommandType;
            command.AddArgsToCommand(cmd.Parameters, cmd.Args);
        }

        public static int ReRunCommand(SqlCommand command, object args)
        {
            var logger = CoPilotGlobalResources.Locator.Get<ILogger>();
            int result;
            var props = args.GetType().GetClassMembers().ToDictionary(k => "@"+k.Name, v => v.GetValue(args));
            try
            {
                lock (LockObj)
                {
                    var timer = Stopwatch.StartNew();

                    command.ReplaceArgsInCommand(props);
                    logger.LogVerbose("Executing Non Query", command.CommandText);
                    result = command.ExecuteNonQuery();
                    var time = timer.ElapsedMilliseconds;
                    logger.LogVerbose($"^ Affected {result} rows in {time}ms");
                }
            }
            catch (Exception ex)
            {
                throw new CoPilotDataException("Unable to execute command!", ex);
            }

            return result;

        }

        public static object ExecuteScalar(SqlConnection connection, DbRequest cmd)
        {
            return ExecuteScalar(new SqlCommand("", connection), cmd);
        }

        public static object ExecuteScalar(SqlCommand command, DbRequest cmd)
        {
            var logger = CoPilotGlobalResources.Locator.Get<ILogger>();
            var timer = Stopwatch.StartNew();
            object result;
            try { 
                lock (LockObj)
                {
                    command.CommandText = cmd.ToString();
                    command.CommandType = cmd.CommandType;
                    command.AddArgsToCommand(cmd.Parameters, cmd.Args);
                    logger.LogVerbose("Executing Scalar", command.CommandText);
                    var time = timer.ElapsedMilliseconds;
                    logger.LogVerbose($"^ Finished in {time}ms");
                    result = command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new CoPilotDataException("Unable to execute command!", ex);
            }
            return result;
        }

        private static IEnumerable<string> SplitSqlStatements(string sqlScript)
        {
            // Split by "GO" statements
            var statements = Regex.Split(
                    sqlScript,
                    @"^\s*GO\s*\d*\s*($|\-\-.*$)",
                    RegexOptions.Multiline |
                    RegexOptions.IgnorePatternWhitespace |
                    RegexOptions.IgnoreCase);

            // Remove empties, trim, and return
            return statements
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim(' ', '\r', '\n'));
        }

    }
}
