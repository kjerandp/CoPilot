using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Database;
using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Database.Commands.Query.Interfaces;
using CoPilot.ORM.Database.Commands.Query.Strategies;
using CoPilot.ORM.Database.Commands.SqlWriters;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Extensions;
using CoPilot.ORM.Filtering;
using CoPilot.ORM.Helpers;
using CoPilot.ORM.Logging;
using CoPilot.ORM.Model;
using CoPilot.ORM.Providers.MySql.Writers;
using MySql.Data.MySqlClient;

namespace CoPilot.ORM.Providers.MySql
{
    public class MySqlProvider : IDbProvider
    {
        private readonly object _lockObj = new object();
        private readonly bool _useNvar;
        private readonly MethodCallConverters _converters;

        public ILogger Logger { get; set; }
        public IModelValidator ModelValidator { get; set; }

        public MySqlProvider(LoggingLevel loggingLevel = LoggingLevel.None, bool useNvar = true)
        {
            _useNvar = useNvar;
            _converters = new MethodCallConverters();

            CreateStatementWriter = new MySqlCreateStatementWriter(this);
            InsertStatementWriter = new MySqlInsertStatementWriter(this);
            UpdateStatementWriter = new MySqlUpdateStatementWriter(this);
            DeleteStatementWriter = new MySqlDeleteStatementWriter(this);
            SelectStatementWriter = new MySqlSelectStatementWriter();
            CommonScriptingTasks = new SqlCommonScriptingTasks();
            QueryBuilder = new MySqlQueryBuilder();
            QueryStrategySelector = new MySqlQueryStrategySelector(QueryBuilder, SelectStatementWriter).Get();

            Logger = new ConsoleLogger {LoggingLevel = loggingLevel};
            ModelValidator = new SimpleModelValidator();
        }
        
        public DbResponse ExecuteQuery(DbRequest cmd, params string[] names)
        {

            var resultSets = new List<DbRecordSet>();
            var timer = Stopwatch.StartNew();
            
            var command = (MySqlCommand) cmd.Command;

            try
            {
                lock (_lockObj)
                {
                    if (command.Connection.State != ConnectionState.Open)
                        command.Connection.Open();

                    command.CommandText = cmd.ToString();
                    command.CommandType = cmd.CommandType;
                    AddArgsToCommand(command, cmd.Parameters, cmd.Args);

                    Logger?.LogVerbose("Executing Query", command.CommandText);

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
            Logger?.LogVerbose($"^ Executed in {time}ms (selected {resultSets.Sum(r => r.Records.Length)} rows)");
            return new DbResponse(resultSets.ToArray(), time);

        }
        
        public int ExecuteNonQuery(DbRequest cmd)
        {
            var result = 0;
            var command = (MySqlCommand) cmd.Command;
            try
            {
                lock (_lockObj)
                {
                    var statements = SplitSqlStatements(cmd.ToString());
                    command.CommandType = cmd.CommandType;
                    AddArgsToCommand(command, cmd.Parameters, cmd.Args);

                    foreach (var statement in statements)
                    {
                        command.CommandText = statement;
                        Logger?.LogVerbose("Executing Non Query", command.CommandText);
                        var timer = Stopwatch.StartNew();
                        var r = command.ExecuteNonQuery();
                        var time = timer.ElapsedMilliseconds;
                        Logger?.LogVerbose($"^ Affected {r} rows in {time}ms");
                        result = r < 0 ? r : result + r;
                    }

                }
            }
            catch (Exception ex)
            {
                throw new CoPilotDataException("Unable to execute command!", ex);
            }
            
            return result;

        }

        public void PrepareNonQuery(DbRequest cmd)
        {
            var command = (MySqlCommand)cmd.Command;
            command.CommandText = string.Join(";\n", SplitSqlStatements(cmd.ToString()));
            command.CommandType = cmd.CommandType;
            AddArgsToCommand(command, cmd.Parameters, cmd.Args);
        }

        public int ReRunCommand(IDbCommand command, object args)
        {
            int result;
            var cmd = (MySqlCommand) command;

            var props = args.GetType().GetClassMembers().ToDictionary(k => "@"+k.Name, v => v.GetValue(args));
            try
            {
                lock (_lockObj)
                {
                    var timer = Stopwatch.StartNew();

                    ReplaceArgsInCommand(cmd, props);
                    Logger?.LogVerbose("Executing Non Query", command.CommandText);
                    result = command.ExecuteNonQuery();
                    var time = timer.ElapsedMilliseconds;
                    Logger?.LogVerbose($"^ Affected {result} rows in {time}ms");
                }
            }
            catch (Exception ex)
            {
                throw new CoPilotDataException("Unable to execute command!", ex);
            }

            return result;

        }
        
        public object ExecuteScalar(DbRequest cmd)
        {
            var timer = Stopwatch.StartNew();
            var command = (MySqlCommand) cmd.Command;
            object result;
            try { 
                lock (_lockObj)
                {
                    command.CommandText = cmd.ToString();
                    command.CommandType = cmd.CommandType;
                    AddArgsToCommand(command, cmd.Parameters, cmd.Args);
                    Logger?.LogVerbose("Executing Scalar", command.CommandText);
                    var time = timer.ElapsedMilliseconds;
                    Logger?.LogVerbose($"^ Finished in {time}ms");
                    result = command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new CoPilotDataException("Unable to execute command!", ex);
            }
            return result;
        }

        

        public ICreateStatementWriter CreateStatementWriter { get; }

        public IQueryBuilder QueryBuilder { get; }

        public QueryStrategySelector QueryStrategySelector { get; }

        public ISelectStatementWriter SelectStatementWriter { get; }

        public IInsertStatementWriter InsertStatementWriter { get; }

        public IUpdateStatementWriter UpdateStatementWriter { get; }

        public IDeleteStatementWriter DeleteStatementWriter { get; }

        public ICommonScriptingTasks CommonScriptingTasks { get; }

        public IDbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        public IDbCommand CreateCommand(IDbConnection connection = null, int timeout = 0)
        {
            var cmd = new MySqlCommand(""){CommandTimeout = timeout};

            var con = connection as MySqlConnection;

            if (con != null)
            {
                cmd.Connection = con;
            }

            return cmd;
        }

        public bool ValidateModel(IDb db)
        {
            return ModelValidator.Validate(db);
        }

        public MemberMethodCallConverter GetMethodCallConverter(string methodName)
        {
            return _converters.GetConverter(methodName);
        }

        public string GetDataTypeAsString(DbDataType dataType, int size = 0)
        {
            var str = GetTypeString(dataType);
            if (str.EndsWith("<length>"))
            {
                var maxSize = size <= 0? "" : "("+size+")";
                str = str.Replace("<length>", $"{maxSize}");
            }
            return str;
        }
        
        public string GetValueAsString(DbDataType dataType, object value)
        {
            if (value == null) return "NULL";

            if (dataType == DbDataType.Boolean)
            {
                return (bool)value ? "1" : "0";
            }
            if (dataType == DbDataType.DateTime)
            {
                var date = (DateTime)value;
                return $"'{date:yyyy-MM-dd HH:mm}'";
            }

            if (dataType == DbDataType.Date)
            {
                var date = (DateTime)value;
                return $"'{date:yyyy-MM-dd HH:mm}'";
            }
            if (DbConversionHelper.IsText(dataType))
            {
                var str = value.ToString().Replace("'", "''");

                double result;
                if (double.TryParse(str, out result))
                {
                    return "'" + str + "'";
                }

                return (_useNvar ? "N'" : "'") + str + "'";
            }

            if (DbConversionHelper.IsNumeric(dataType))
            {

                if (value.GetType().GetTypeInfo().IsEnum)
                {
                    return ((int)value).ToString();
                }

                return value.ToString()
                        .Replace("'", "")
                        .Replace("/*", "")
                        .Replace("*\\", "")
                        .Replace("--", "")
                        .Replace(";", "")
                        .Replace(" ", "")
                        .Replace(",", ".");

            }

            throw new CoPilotUnsupportedException($"Unable to convert {dataType} to a string.");
        }

        private string GetTypeString(DbDataType type)
        {
            switch (type)
            {
                case DbDataType.Int64: return "BIGINT";
                case DbDataType.Binary: return "BINARY";
                case DbDataType.Varbinary: return "VARBINARY";
                case DbDataType.Boolean: return "BIT";
                case DbDataType.Char: return "CHAR<length>";
                case DbDataType.Date: return "DATE";
                case DbDataType.DateTime: return "DATETIME";
                case DbDataType.DateTimeOffset: throw new CoPilotUnsupportedException(type.ToString());
                case DbDataType.Decimal: return "DECIMAL<precision>";
                case DbDataType.Double: return "DOUBLE";
                case DbDataType.Int32: return "INT";
                case DbDataType.Currency: return "DECIMAL(10,2)";
                case DbDataType.Text: return "TEXT";
                case DbDataType.String: return "VARCHAR<length>";
                case DbDataType.Float: return "FLOAT";
                case DbDataType.Int16: return "SMALLINT";
                case DbDataType.TimeSpan: return "TIME";
                case DbDataType.TimeStamp: return "TIMESTAMP";
                case DbDataType.Byte: return "TINYINT";
                case DbDataType.Guid: return "CHAR(16) BINARY";
                case DbDataType.Xml: return "TEXT";
                case DbDataType.Enum: return "INT";

                default:
                    return "CHAR";
            }
        }
        private MySqlDbType ToDbType(DbDataType type)
        {
            switch (type)
            {
                case DbDataType.Int64: return MySqlDbType.Int64;
                case DbDataType.Binary: return MySqlDbType.Binary;
                case DbDataType.Varbinary: return MySqlDbType.VarBinary;
                case DbDataType.Boolean: return MySqlDbType.Bit;
                case DbDataType.Char: return MySqlDbType.VarChar;
                case DbDataType.Date: return MySqlDbType.Date;
                case DbDataType.DateTime: return MySqlDbType.DateTime;
                case DbDataType.DateTimeOffset: throw new CoPilotUnsupportedException(type.ToString());
                case DbDataType.Decimal: return MySqlDbType.Decimal;
                case DbDataType.Double: return MySqlDbType.Double;
                case DbDataType.Int32: return MySqlDbType.Int32;
                case DbDataType.Currency: return MySqlDbType.Decimal;
                case DbDataType.Text: return MySqlDbType.Text;
                case DbDataType.String: return MySqlDbType.String;
                case DbDataType.Float: return MySqlDbType.Float;
                case DbDataType.Int16: return MySqlDbType.Int16;
                case DbDataType.TimeSpan: return MySqlDbType.Time;
                case DbDataType.TimeStamp: return MySqlDbType.Timestamp;
                case DbDataType.Byte: return MySqlDbType.Byte;
                case DbDataType.Guid: return MySqlDbType.Guid;
                case DbDataType.Xml: return MySqlDbType.Text;
                case DbDataType.Enum: return MySqlDbType.Int32; 

                default:
                    return MySqlDbType.VarChar;
            }
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

        private void AddArgsToCommand(MySqlCommand command, IReadOnlyCollection<DbParameter> parameters, IReadOnlyDictionary<string, object> args)
        {
            if (command.Parameters.Count > 0) command.Parameters.Clear();

            if (parameters == null) return;
            foreach (var param in parameters)
            {
                if (param.IsOutput)
                {
                    command.Parameters.Add(param.Name, ToDbType(param.DataType), param.Size).Direction = ParameterDirection.Output;
                }
                else
                {
                    if (!args.ContainsKey(param.Name)) continue;

                    var enumerable = args[param.Name] as ICollection<object>;
                    if (enumerable != null)
                    {
                        AddArrayParameters(command, param.Name, enumerable);
                    }
                    else
                    {
                        command.Parameters.AddWithValue(param.Name, args[param.Name] ?? DBNull.Value);
                    }
                }

            }
        }

        private static void ReplaceArgsInCommand(MySqlCommand command, IReadOnlyDictionary<string, object> args)
        {
            if (command.Parameters == null || command.Parameters.Count == 0) throw new CoPilotUnsupportedException("Command doesn't have any parameters defined!");

            for (var i = 0; i < command.Parameters.Count; i++)
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

        private static void AddArrayParameters<T>(MySqlCommand cmd, string name, IEnumerable<T> values)
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
    }
}