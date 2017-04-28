using System;

namespace CoPilot.ORM.Config.DataTypes
{
    public class DefaultValue
    {
        public static DefaultValue PrimaryKey => new DefaultValue(DbExpressionType.PrimaryKeySequence);

        public DefaultValue(object value)
        {
            Value = value;
            Expression = DbExpressionType.Constant;
        }

        public DefaultValue(DbExpressionType expression, object value = null)
        {
            Expression = expression;
            Value = value;
        }

        public DbExpressionType Expression { get; private set; }
        public object Value { get; private set; }

        public object CreateDefaultValue()
        {
            switch (Expression)
            {
                case DbExpressionType.Timestamp:
                    return null;
                case DbExpressionType.CurrentDate:
                    return DateTime.UtcNow.Date;
                case DbExpressionType.CurrentDateTime:
                    return DateTime.UtcNow;
                case DbExpressionType.Guid:
                case DbExpressionType.SequencialGuid:
                    return Guid.NewGuid();
                case DbExpressionType.PrimaryKeySequence:
                    return null;
                default: return Value;
            }
        }
    }
}
