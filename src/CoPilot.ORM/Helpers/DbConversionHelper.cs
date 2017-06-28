using System;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Extensions;
using System.Reflection;


namespace CoPilot.ORM.Helpers
{
    public static class DbConversionHelper
    {
        public const int DefaultDbStringSize = 255;

        public static DbDataType GetDbDataType(ClassMemberInfo member)
        {
            var type = member.MemberType;
            return MapToDbDataType(type);
        }

        public static DbDataType MapToDbDataType(Type type)
        {
            if (!type.GetTypeInfo().IsValueType)
            {
                if (type == typeof(string)) return DbDataType.String;
                if (type == typeof(byte[])) return DbDataType.Varbinary;
                //if (type == typeof(XmlDocument)) return DbDataType.Xml;
                if (type.IsReference()) return DbDataType.Reference;
                if (type.IsCollection()) return DbDataType.Collection;
            }
            else
            {
                if (type.GetTypeInfo().IsEnum) return DbDataType.Enum;
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
                //case DbDataType.Xml: return typeof(XmlDocument);
                case DbDataType.Enum: return typeof(Enum);
                default: return typeof(object);
            }
        }

        public static bool IsNumeric(DbDataType dataType)
        {
            return (dataType == DbDataType.Int16 || dataType == DbDataType.Int32 || dataType == DbDataType.Int64 ||
                    dataType == DbDataType.Byte);
        }

        public static bool IsText(DbDataType dataType)
        {
            return (
                dataType == DbDataType.Char ||
                dataType == DbDataType.Text ||
                dataType == DbDataType.String
            );
        }

        public static bool DataTypeHasSize(DbDataType dataType) //should probably be provider specific
        {
            return (
                dataType == DbDataType.Char ||
                dataType == DbDataType.String || 
                dataType == DbDataType.Varbinary ||
                dataType == DbDataType.Binary
            );
        }
    }
}
