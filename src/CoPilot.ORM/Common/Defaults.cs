using CoPilot.ORM.Exceptions;

namespace CoPilot.ORM.Common
{
    public static class Defaults
    {
        public static string GetOperatorAsText(SqlOperator op)
        {
            switch (op)
            {
                case SqlOperator.AndAlso: return "AND";
                case SqlOperator.OrElse: return "OR";
                case SqlOperator.Equal: return "=";
                case SqlOperator.NotEqual: return "<>";
                case SqlOperator.GreaterThan: return ">";
                case SqlOperator.GreaterThanOrEqual: return ">=";
                case SqlOperator.LessThan: return "<";
                case SqlOperator.LessThanOrEqual: return "<=";
                case SqlOperator.Add: return "+";
                case SqlOperator.Subtract: return "-";
                case SqlOperator.Like: return "LIKE";
                case SqlOperator.NotLike: return "NOT LIKE";
                case SqlOperator.Is: return "IS";
                case SqlOperator.IsNot: return "IS NOT";
                case SqlOperator.In: return "IN";
                case SqlOperator.NotIn: return "NOT IN";
                default: throw new CoPilotUnsupportedException(op.ToString());
            }
        }

        
    }
}
