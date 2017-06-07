using CoPilot.ORM.Filtering.Operands;

namespace CoPilot.ORM.Filtering
{
    public delegate void MemberMethodCallConverter(object[] args, ConversionResult result);

    
    public class ConversionResult
    {
        public ConversionResult(MemberExpressionOperand memberExpressionOperand)
        {
            MemberExpressionOperand = memberExpressionOperand;
        }

        public MemberExpressionOperand MemberExpressionOperand { get; }
        public object Value { get; set; }
        public string Operator { get; set; }
    }
}