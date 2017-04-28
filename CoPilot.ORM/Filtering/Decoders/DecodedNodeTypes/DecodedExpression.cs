using System.Linq.Expressions;
using CoPilot.ORM.Filtering.Decoders.Interfaces;

namespace CoPilot.ORM.Filtering.Decoders.DecodedNodeTypes
{
    public class DecodedExpression: IDecodedNode
    {
        public DecodedExpression(ExpressionType operand, IDecodedNode left, IDecodedNode right)
        {
            Operand = operand;
            Left = left;
            Right = right;
        }

        public ExpressionType Operand { get; }
        public IDecodedNode Left { get; }
        public IDecodedNode Right { get; }
    }
}