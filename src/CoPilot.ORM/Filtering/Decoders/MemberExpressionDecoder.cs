using System;
using System.Linq.Expressions;
using CoPilot.ORM.Filtering.Decoders.DecodedNodeTypes;
using CoPilot.ORM.Filtering.Decoders.Interfaces;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Filtering.Decoders
{
    public class MemberExpressionDecoder : IExpressionDecoder
    {
        private readonly MemberExpression _expression;

        internal MemberExpressionDecoder(MemberExpression expression)
        {
            _expression = expression;
        }
        public IDecodedNode Decode()
        {
            if (_expression.NodeType == ExpressionType.MemberAccess)
            {
                var member = _expression; 
                while (member != null)
                {
                    var constant = member.Expression as ConstantExpression;
                    if (constant != null || member.Expression == null)
                    {
                        object value;
                        if (ExpressionHelper.TryDynamicallyInvokeExpression(_expression, out value))
                        {
                            if(value == null) return new DecodedNullValue();
                            return new DecodedValue(value.GetType(), value );
                        }
                    }
                    else
                    {
                        var param = member.Expression as ParameterExpression;
                        if (param != null)
                        {
                            var memberPath = PathHelper.RemoveFirstElementFromPathString(_expression.ToString());
                            //if (string.IsNullOrEmpty(memberPath)) throw new ArgumentException("Invalid member expression!");
                            return new DecodedReference(param.Type, memberPath);
                        }
                    }
                    
                    member = member.Expression as MemberExpression;
                }
            }
            throw new ArgumentException("Unable to decode member expression!");
        }
    }
}