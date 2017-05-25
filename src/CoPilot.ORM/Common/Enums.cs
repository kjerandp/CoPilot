using System;

namespace CoPilot.ORM.Common
{
    public enum TableJoinType
    {
        InnerJoin,
        LeftJoin
    }

    [Obsolete]
    public enum SqlCommandType
    {
        SqlStatement,
        StoredProcedure
    }

    public enum Ordering
    {
        Ascending,
        Descending
    }

    [Flags]
    public enum OperationType
    {
        Select = 0x1,
        Update = 0x2,
        Insert = 0x4,
        Delete = 0x8,
        All = (Select|Update|Insert|Delete)
    }

    public enum LoggingLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Verbose = 4
    }
}
