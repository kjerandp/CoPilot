using CoPilot.ORM.Common;
using CoPilot.ORM.Filtering.Interfaces;

namespace CoPilot.ORM.Filtering.Operands
{
    public class BinaryOperand : IExpressionOperand
    {
        public BinaryOperand(IExpressionOperand left, IExpressionOperand right, SqlOperator op)
        {
            Left = left;
            Right = right;
            Operator = op;
        }

        public IExpressionOperand Left { get; set; }
        public IExpressionOperand Right { get; set; }
        public bool Enclose { get; set; }
        public SqlOperator Operator { get; set; }

        public override string ToString()
        {
            var str = $"{Left} {Defaults.GetOperatorAsText(Operator)} {Right}";
            if (Enclose)
            {
                str = $"({str})";
            }
            return str;
        }
    }
}