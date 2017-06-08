using CoPilot.ORM.Context;
using CoPilot.ORM.Filtering.Interfaces;

namespace CoPilot.ORM.Filtering.Operands
{
    public class ContextMemberOperand : IExpressionOperand
    {
        public ContextMemberOperand(MemberExpressionOperand memberExpression)
        {
            MemberExpressionOperand = memberExpression;
        }

        public ContextColumn ContextColumn { get; set; }
        public MemberExpressionOperand MemberExpressionOperand { get; }

        public override string ToString()
        {
            var str = $"T{ContextColumn.Node.Index}.{ContextColumn.Column.ColumnName}";
            if (!string.IsNullOrEmpty(MemberExpressionOperand?.Custom))
            {
                return MemberExpressionOperand.Custom.Replace("{column}", str);
            }
            if (!string.IsNullOrEmpty(MemberExpressionOperand?.WrapWith))
            {
                str = $"{MemberExpressionOperand.WrapWith}({str})";
            }
            return str;

        }
    }
}