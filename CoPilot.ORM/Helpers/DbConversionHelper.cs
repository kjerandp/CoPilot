using System;
using System.Data;
using System.Xml;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Extensions;


namespace CoPilot.ORM.Helpers
{
    public static class DbConversionHelper
    {
        public const bool DefaultUseNvarForString = true;
        public const string DefaultDbStringSize = "255";

        public static DbDataType GetDbDataType(ClassMemberInfo member)
        {
            var type = member.MemberType;
            return MapToDbDataType(type);
        }

        internal static DbDataType MapToDbDataType(Type type)
        {
            if (!type.IsValueType)
            {
                if (type == typeof(string)) return DbDataType.String;
                if (type == typeof(byte[])) return DbDataType.Varbinary;
                if (type == typeof(XmlDocument)) return DbDataType.Xml;
                if (type.IsReference()) return DbDataType.Reference;
                if (type.IsCollection()) return DbDataType.Collection;
            }
            else
            {
                if (type.IsEnum) return DbDataType.Enum;
                if (type == typeof(bool) || type == typeof(bool?)) return DbDataType.Boolean;
                if (type == typeof(decimal) || type == typeof(decimal?)) return DbDataType.Decimal;
                if (type == typeof(float) || type == typeof(float?)) return DbDataType.Float;
                if (type == typeof(short) || type == typeof(short?)) return DbDataType.Int16;
                if (type == typeof(int) || type == typeof(int?)) return DbDataType.Int32;
                if (type == typeof(long) || type == typeof(long?)) return DbDataType.Int64;
                if (type == typeof(DateTime) || type == typeof(DateTime?)) return DbDataType.DateTime;
                if (type == typeof(TimeSpan) || type == typeof(TimeSpan?)) return DbDataType.TimeSpan;
                if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?)) return DbDataType.DateTimeOffset;
                if (type == typeof(byte) || type == typeof(byte?)) return DbDataType.Byte;
                if (type == typeof(char) || type == typeof(char?)) return DbDataType.Char;
                if (type == typeof(Guid) || type == typeof(Guid?)) return DbDataType.Guid;
            }
            return DbDataType.Unknown;
        }

        internal static Type MapDbToRuntimeDataType(DbDataType type)
        {
            switch (type)
            {
                case DbDataType.Int64: return typeof(long);
                case DbDataType.Binary: return typeof(byte[]);
                case DbDataType.Varbinary: return typeof(byte[]);
                case DbDataType.Boolean: return typeof(bool);
                case DbDataType.Char: return typeof(char);
                case DbDataType.Date: return typeof(DateTime);
                case DbDataType.DateTime: return typeof(DateTime);
                case DbDataType.DateTimeOffset: return typeof(DateTimeOffset);
                case DbDataType.Decimal: return typeof(decimal);
                case DbDataType.Double: return typeof(double);
                case DbDataType.Int32: return typeof(int);
                case DbDataType.Currency: return typeof(decimal);
                case DbDataType.Text: return typeof(string);
                case DbDataType.String: return typeof(string);
                case DbDataType.Float: return typeof(double);
                case DbDataType.Int16: return typeof(short);
                case DbDataType.TimeSpan: return typeof(TimeSpan);
                case DbDataType.TimeStamp: return typeof(byte[]);
                case DbDataType.Byte: return typeof(byte);
                case DbDataType.Guid: return typeof(Guid);
                case DbDataType.Xml: return typeof(XmlDocument);
                case DbDataType.Enum: return typeof(Enum);
                default: return typeof(object);
            }
        }

        

        public static SqlDbType ToDbType(DbDataType type, bool useNvar = DefaultUseNvarForString)
        {
            switch (type)
            {
                case DbDataType.Int64: return SqlDbType.BigInt;
                case DbDataType.Binary: return SqlDbType.Binary;
                case DbDataType.Varbinary: return SqlDbType.VarBinary;
                case DbDataType.Boolean: return SqlDbType.Bit;
                case DbDataType.Char: return useNvar ? SqlDbType.NChar : SqlDbType.Char;
                case DbDataType.Date: return SqlDbType.Date;
                case DbDataType.DateTime: return SqlDbType.DateTime2;
                case DbDataType.DateTimeOffset: return SqlDbType.DateTimeOffset;
                case DbDataType.Decimal: return SqlDbType.Decimal;
                case DbDataType.Double: return SqlDbType.Float;
                case DbDataType.Int32: return SqlDbType.Int;
                case DbDataType.Currency: return SqlDbType.Money;
                case DbDataType.Text: return useNvar ? SqlDbType.NText : SqlDbType.Text;
                case DbDataType.String: return useNvar ? SqlDbType.NVarChar : SqlDbType.VarChar;
                case DbDataType.Float: return SqlDbType.Real;
                case DbDataType.Int16: return SqlDbType.SmallInt;
                case DbDataType.TimeSpan: return SqlDbType.Time;
                case DbDataType.TimeStamp: return SqlDbType.Timestamp;
                case DbDataType.Byte: return SqlDbType.TinyInt;
                case DbDataType.Guid: return SqlDbType.UniqueIdentifier;
                case DbDataType.Xml: return SqlDbType.Xml;
                case DbDataType.Enum: return SqlDbType.SmallInt;

                default:
                    return SqlDbType.Char;
            }
        }

        public static string GetAsString(DbDataType dataType, bool useNvar = DefaultUseNvarForString)
        {
            return ToDbType(dataType, useNvar).ToString().ToLowerInvariant();
        }

        public static string GetExpressionAsString(DbExpressionType expression)
        {
            switch (expression)
            {
                case DbExpressionType.Timestamp:
                    return "GETDATE()";
                case DbExpressionType.CurrentDate:
                    return "GETDATE()";
                case DbExpressionType.CurrentDateTime:
                    return "GETDATE()";
                case DbExpressionType.Guid:
                    return "NEWID()";
                case DbExpressionType.SequencialGuid:
                    return "NEWSEQUENTIALID()";
                case DbExpressionType.PrimaryKeySequence:
                    return "IDENTITY(1,1)";
                default: return null;
            }
        }

        public static string GetValueAsString(DbDataType dataType, object value, bool useNvar = DefaultUseNvarForString)
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
            if (IsText(dataType))
            {
                var str = value.ToString().Replace("'", "''");

                double result;
                if (double.TryParse(str, out result))
                {
                    return "'" + str + "'";
                }

                return (useNvar?"N'":"'") + str + "'";
            }

            if (IsNumeric(dataType))
            {

                if (value.GetType().IsEnum)
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

            throw new ArgumentException($"Unable to convert {dataType} to a string.");
        }

        public static bool IsNumeric(DbDataType dataType)
        {
            var sqlType = ToDbType(dataType);

            return (
                sqlType == SqlDbType.TinyInt ||
                sqlType == SqlDbType.SmallInt ||
                sqlType == SqlDbType.Int ||
                sqlType == SqlDbType.BigInt
            );
        }

        public static bool IsText(DbDataType dataType)
        {
            var sqlType = ToDbType(dataType);

            return (
                sqlType == SqlDbType.Char ||
                sqlType == SqlDbType.NChar ||
                sqlType == SqlDbType.Text ||
                sqlType == SqlDbType.NText ||
                sqlType == SqlDbType.VarChar ||
                sqlType == SqlDbType.NVarChar
            );
        }

        public static bool HasSize(DbDataType dataType)
        {
            var sqlType = ToDbType(dataType);

            return (
                IsText(dataType) ||
                sqlType == SqlDbType.VarBinary ||
                sqlType == SqlDbType.Binary
            );
        }
    }
}
