using System;
using System.Linq.Expressions;
using CoPilot.ORM.Filtering.Decoders.DecodedNodeTypes;
using CoPilot.ORM.Filtering.Decoders.Interfaces;
using CoPilot.ORM.Helpers;

namespace CoPilot.ORM.Filtering.Decoders
{
    public class UnaryExpressionDecoder : IExpressionDecoder
    {
        private readonly UnaryExpression _expression;

        internal UnaryExpressionDecoder(UnaryExpression expression)
        {
            _expression = expression;
        }
        public IDecodedNode Decode()
        {
            object value;
            if (ExpressionHelper.TryDynamicallyInvokeExpression(_expression, out value))
            {
                if (value == null) return new DecodedNullValue();
                return new DecodedValue(value.GetType(), value);
            }

            var decoder = ExpressionTypeResolver.Get(_expression.Operand);
            var result = decoder.Decode();

            //var valueResult = result as DecodedValue;
            //if (valueResult != null)
            //{
            //    switch (_expression.NodeType)
            //    {
            //        case ExpressionType.Not:
            //            {
            //                if (valueResult.ValueType == typeof(bool) || valueResult.ValueType == typeof(bool?))
            //                {
            //                    var val = valueResult.Value != null && ((bool?)valueResult.Value == true);
            //                    return new DecodedValue(valueResult.ValueType, !val);
            //                }
            //                throw new ArgumentException($"Value type not supported for the NOT operator! {valueResult.ValueType.Name}");
            //            }
            //        case ExpressionType.Convert: return valueResult;
            //        default: throw new ArgumentException($"Unary expression operator '{_expression.NodeType}' not supported for this node type! ({result.GetType().Name})");
            //    }
            //}
            var refResult = result as DecodedReference;
            if (refResult != null)
            {
                switch (_expression.NodeType)
                {
                    case ExpressionType.Not:
                    {
                        if (string.IsNullOrEmpty(refResult.ReferencedTypeMemberAccess))
                        {
                            if (refResult.ReferencedType == typeof(bool) || refResult.ReferencedType == typeof(bool?))
                            {
                                var transformedRef = new DecodedReference(refResult.BaseType, refResult.Path);
                                return new DecodedExpression(ExpressionType.Equal, transformedRef, new DecodedValue(typeof(bool), false));
                            }
                        }
                        else if (refResult.ReferencedTypeMemberAccess == "HasValue")
                        {
                            var transformedRef = new DecodedReference(refResult.BaseType, refResult.Path);
                            var nullValue = new DecodedNullValue();
                            return new DecodedExpression(ExpressionType.Equal, transformedRef, nullValue);
                        }
                        throw new ArgumentException($"Member type not supported for the NOT operator! {refResult.Path}");
                    }
                    case ExpressionType.Convert: return refResult;
                            
                        
                    default: throw new ArgumentException($"Unary expression operator '{_expression.NodeType}' not supported for this node type! ({result.GetType().Name})");
                }
            }
            throw new ArgumentException($"Node not supported as part of unary expression! {result.GetType().Name}");
        }
    }
}