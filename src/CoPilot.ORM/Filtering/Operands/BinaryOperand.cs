using CoPilot.ORM.Filtering.Interfaces;

namespace CoPilot.ORM.Filtering.Operands
{
    public class BinaryOperand : IExpressionOperand
    {
        public BinaryOperand(IExpressionOperand left, IExpressionOperand right, string op)
        {
            Left = left;
            Right = right;
            Operator = op;
        }

        public IExpressionOperand Left { get; set; }
        public IExpressionOperand Right { get; set; }
        public bool Enclose { get; set; }
        public string Operator { get; set; }

        public override string ToString()
        {
            var str = $"{Left} {Operator} {Right}";
            if (Enclose)
            {
                str = $"({str})";
            }
            return str;
        }
    }
}