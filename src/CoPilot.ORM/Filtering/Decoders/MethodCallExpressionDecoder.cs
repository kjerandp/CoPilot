using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering.Decoders.DecodedNodeTypes;
using CoPilot.ORM.Filtering.Decoders.Interfaces;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Filtering.Decoders
{
    public class MethodCallExpressionDecoder : IExpressionDecoder
    {
        private readonly MethodCallExpression _expression;

        internal MethodCallExpressionDecoder(MethodCallExpression expression)
        {
            _expression = expression;
        }
        public IDecodedNode Decode()
        {
            var obj = _expression.Object != null ? ExpressionTypeResolver.Get(_expression.Object).Decode():null;
            var args = _expression.Arguments?.Select(r => ExpressionTypeResolver.Get(r).Decode()).ToArray();

            if (args.OfType<DecodedReference>().Any())
            {
                throw new CoPilotUnsupportedException($"Not supported method call: {_expression.Method.Name}. Reference node part of arguments!");
            }

            var refNode = obj as DecodedReference;
            if (refNode != null)
            {
                var objArgs = new List<object>(args.Length);
                foreach (var arg in args)
                {
                    var nullArg = arg as DecodedNullValue;
                    if(nullArg != null) objArgs.Add(null);
                    var valArg = arg as DecodedValue;
                    if (valArg != null) objArgs.Add(valArg.Value);

                }
                refNode.ReferencedTypeMethodCall = _expression.Method.Name;
                refNode.ReferenceTypeMethodCallArgs = objArgs.ToArray();
                return refNode;
            }
            

            object value;
            if (ExpressionHelper.TryDynamicallyInvokeExpression(_expression, out value))
            {
                if(value == null) return new DecodedNullValue();
                return new DecodedValue(value.GetType(), value);
            }
            
            throw new CoPilotUnsupportedException("Expression not supported!");
        }
    }
}