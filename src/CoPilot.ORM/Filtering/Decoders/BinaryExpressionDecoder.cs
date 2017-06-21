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
            var left = FilterExpressionTypeResolver.Get(_expression.Left).Decode();
            
            var right = FilterExpressionTypeResolver.Get(_expression.Right).Decode();

            if (left is DecodedReference && right is DecodedExpression)
            {
                left = ConvertIfBool((DecodedReference)left);

            } else if (right is DecodedReference && left is DecodedExpression)
            {
                right = ConvertIfBool((DecodedReference)right);
            }
            
            return new DecodedExpression(_expression.NodeType, left, right);
        }

        private IDecodedNode ConvertIfBool(DecodedReference reference)
        {
            if (reference.ReferencedType == typeof(bool) || reference.ReferencedType == typeof(bool?))
            {
                return ExpressionDecoder.TransformBooleanReferenceToBinaryExpression(reference, true);
            }
            return reference;
        }
    }
}
