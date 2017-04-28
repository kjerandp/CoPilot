using System.Linq.Expressions;
using CoPilot.ORM.Filtering.Decoders.DecodedNodeTypes;
using CoPilot.ORM.Filtering.Decoders.Interfaces;

namespace CoPilot.ORM.Filtering.Decoders
{
    public class ConstantExpressionDecoder : IExpressionDecoder
    {
        private readonly ConstantExpression _expression;

        internal ConstantExpressionDecoder(ConstantExpression expression)
        {
            _expression = expression;
        }
        public IDecodedNode Decode()
        {
            if(_expression.Value == null) return new DecodedNullValue();

            var result = new DecodedValue(_expression.Type, _expression.Value);
            return result;
        }
    }
}