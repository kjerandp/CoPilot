using System;

namespace CoPilot.ORM.Common
{
    public enum TableJoinType
    {
        InnerJoin,
        LeftJoin
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

    public enum SqlOperator
    {
        AndAlso,
        OrElse,
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Add,
        Subtract,
        Like,
        NotLike,
        Is,
        IsNot,
        In,
        NotIn
    }
}
