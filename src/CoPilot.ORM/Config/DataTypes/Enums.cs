namespace CoPilot.ORM.Config.DataTypes
{
    public enum DbExpressionType
    {
        Constant,
        PrimaryKeySequence,
        Guid,
        SequencialGuid,
        CurrentDate,
        CurrentDateTime,
        Timestamp
    }

    public enum SettingType
    {
        DefaultVarcharSize,
        DefaultNumberPrecision,
        DefaultValueForPrimaryKeys
    }

    public enum DbDataType
    {
        Boolean,
        Decimal,
        Float,
        Double,
        Int16,
        Int32,
        Int64,
        Currency,
        Date,
        DateTime,
        TimeSpan,
        TimeStamp,
        DateTimeOffset,
        Byte,
        Binary,
        Varbinary,
        Char,
        Text,
        String,
        Guid,
        Xml,
        Enum,
        Reference,
        Collection,
        Unknown
    }

    public enum MappingTarget
    {
        Database,
        Object
    }
}