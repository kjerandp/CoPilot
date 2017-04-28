using System.Linq.Expressions;
using CoPilot.ORM.Filtering.Decoders.DecodedNodeTypes;
using CoPilot.ORM.Filtering.Decoders.Interfaces;

namespace CoPilot.ORM.Filtering.Decoders
{
    public class BinaryExpressionDecoder : IExpressionDecoder
    {
        private readonly BinaryExpression _expression;

        internal BinaryExpressionDecoder(BinaryExpression expression)
        {
            _expression = expression;
        }
        public IDecodedNode Decode()
        {
            
            var left = ExpressionTypeResolver.Get(_expression.Left).Decode();
            
            var right = ExpressionTypeResolver.Get(_expression.Right).Decode();

            return new DecodedExpression(_expression.NodeType, left, right);
        }
    }
}
