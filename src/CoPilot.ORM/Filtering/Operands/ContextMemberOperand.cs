using CoPilot.ORM.Context;
using CoPilot.ORM.Filtering.Interfaces;

namespace CoPilot.ORM.Filtering.Operands
{
    public class ContextMemberOperand : IExpressionOperand
    {
        private readonly MemberExpressionOperand _memberExpression;
        public ContextMemberOperand(MemberExpressionOperand memberExpression)
        {
            _memberExpression = memberExpression;
        }

        public ContextColumn ContextColumn { get; internal set; }
        

        public override string ToString()
        {
            var str = $"T{ContextColumn.Node.Index}.{ContextColumn.Column.ColumnName}";
            if (!string.IsNullOrEmpty(_memberExpression?.Custom))
            {
                return _memberExpression.Custom.Replace("{column}", str);
            }
            if (!string.IsNullOrEmpty(_memberExpression?.WrapWith))
            {
                str = $"{_memberExpression.WrapWith}({str})";
            }
            return str;
        }

    }
}