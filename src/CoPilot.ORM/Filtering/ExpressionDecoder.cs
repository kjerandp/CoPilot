using System.Linq.Expressions;
using CoPilot.ORM.Common;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering.Decoders;
using CoPilot.ORM.Filtering.Decoders.DecodedNodeTypes;
using CoPilot.ORM.Filtering.Decoders.Interfaces;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Filtering.Operands;

namespace CoPilot.ORM.Filtering
{
    public class ExpressionDecoder
    {
        private ExpressionGraph _graph;
        private int _paramIndex;
        private readonly MethodCallConverters _methodCallConverters;

        public ExpressionDecoder(IDbProvider provider)
        {
            _methodCallConverters = MethodCallConverters.Create(provider);
        }

        public ExpressionGraph Decode(Expression expression)
        {
            _graph = new ExpressionGraph();
            _paramIndex = 1;

            var decoder = FilterExpressionTypeResolver.Get(expression);
            var root = decoder.Decode();

            var result = ConvertNode(root);
            var binResult = result as BinaryOperand ?? ConvertToBinaryOperand(result);

            _graph.Root = binResult;

            return _graph;
        }

        private BinaryOperand ConvertToBinaryOperand(IExpressionOperand op)
        {
            var valOp = op as ValueOperand;
            var test = valOp?.Value as bool?;
            valOp = new ValueOperand("@param1", 1);

            if (test == true)
            {
                return new BinaryOperand(valOp, valOp, SqlOperator.Equal);
            }
            
            return new BinaryOperand(valOp, valOp, SqlOperator.NotEqual);
        }
        
        private IExpressionOperand ConvertNode(IDecodedNode node) 
        {
            var nullNode = node as DecodedNullValue;
            if (nullNode != null)
            {
                return new NullOperand();
            }

            var valueNode = node as DecodedValue;
            if (valueNode != null)
            {
                var paramName = $"@param{_paramIndex++}";
                var value = valueNode.Value;
                var op = new ValueOperand(paramName, value);

                return op;
            }

            var refNode = node as DecodedReference;
            if (refNode != null)
            {
                var op = new MemberExpressionOperand(refNode.Path);
                if (!string.IsNullOrEmpty(refNode.ReferencedTypeMemberAccess))
                {
                    if (refNode.ReferencedTypeMemberAccess == "HasValue")
                    {
                        return new BinaryOperand(op, new NullOperand(), SqlOperator.IsNot);
                    }
                    if (refNode.ReferencedTypeMemberAccess == "Date")
                    {
                        op.Custom = "CONVERT(date, {column})"; //TODO make provider specific
                    }
                    else
                    {
                        throw new CoPilotUnsupportedException(
                            $"Unsupported member access on referenced type! ({refNode.ReferencedTypeMemberAccess})");
                    }
                }
                if (!string.IsNullOrEmpty(refNode.ReferencedTypeMethodCall))
                {
                    var converter = _methodCallConverters.GetConverter(refNode.ReferencedTypeMethodCall);
                    var result = new ConversionResult(op);
                    converter.Invoke(refNode.ReferenceTypeMethodCallArgs, result);

                    if (result.Value != null && result.Operator.HasValue)
                    {
                        var valueOpterand = new ValueOperand($"@param{_paramIndex++}", result.Value);
                        if (refNode.IsInverted)
                        {
                            result.Operator = InvertSqlOperator(result.Operator.Value);
                        }
                        return new BinaryOperand(result.MemberExpressionOperand, valueOpterand, result.Operator.Value);
                    }
                    return result.MemberExpressionOperand;
                }
                return op;
            }
            
            var binaryNode = node as DecodedExpression;

            if (binaryNode != null)
            {
                var opType = Translate(binaryNode.Operand);
                var binOp = new BinaryOperand(
                    ConvertNode(binaryNode.Left),
                    ConvertNode(binaryNode.Right),
                    opType
                );
                
                var leftNull = binaryNode.Left as DecodedNullValue;
                var rightNull = binaryNode.Right as DecodedNullValue;

                if (leftNull != null || rightNull != null)
                {
                    binOp.Operator = SqlOperator.Is;
                    if (binaryNode.Operand == ExpressionType.NotEqual)
                    {
                        binOp.Operator = SqlOperator.IsNot;
                    }
                }

                SanitizeBinaryOperand(ref binOp);

                return binOp;
            }
            throw new CoPilotRuntimeException("Unable to decode " + node.GetType().AssemblyQualifiedName);
        }

        private void SanitizeBinaryOperand(ref BinaryOperand binOp)
        {
            var binLeft = binOp.Left as BinaryOperand;
            var binRight = binOp.Right as BinaryOperand;

            BinaryOperand result = null;

            if (binLeft != null && binRight == null)
            {
                var valOp = binOp.Right as ValueOperand;
                result = ConvertExpression(binLeft, valOp);
            }
            else if(binRight != null && binLeft == null)
            {
                var valOp = binOp.Left as ValueOperand;
                result = ConvertExpression(binRight, valOp);
            }

            if(result != null) binOp = result;
        }

        private BinaryOperand ConvertExpression(BinaryOperand binLeft, ValueOperand valOp)
        {
            if (valOp != null && (valOp.Value is bool || valOp.Value is bool?))
            {
                if (valOp.Value as bool? == false)
                {
                    binLeft.Operator = InvertSqlOperator(binLeft.Operator);
                }
                return binLeft;
            }
            return null;
        }

        private static SqlOperator Translate(ExpressionType expressionType) 
        {

            switch (expressionType)
            {
                case ExpressionType.AndAlso: return SqlOperator.AndAlso;
                case ExpressionType.OrElse: return SqlOperator.OrElse;
                case ExpressionType.Equal: return SqlOperator.Equal;
                case ExpressionType.NotEqual: return SqlOperator.NotEqual;
                case ExpressionType.GreaterThan: return SqlOperator.GreaterThan;
                case ExpressionType.GreaterThanOrEqual: return SqlOperator.GreaterThanOrEqual;
                case ExpressionType.LessThan: return SqlOperator.LessThan;
                case ExpressionType.LessThanOrEqual: return SqlOperator.LessThanOrEqual;
                case ExpressionType.Add: return SqlOperator.Add;
                case ExpressionType.Subtract: return SqlOperator.Subtract;
                default: throw new CoPilotUnsupportedException(expressionType.ToString());
            }
        }

        private static SqlOperator InvertSqlOperator(SqlOperator op)
        {
            switch (op)
            {
                case SqlOperator.AndAlso: return SqlOperator.OrElse;
                case SqlOperator.OrElse: return SqlOperator.AndAlso;
                case SqlOperator.Equal: return SqlOperator.NotEqual;
                case SqlOperator.NotEqual: return SqlOperator.Equal;
                case SqlOperator.GreaterThan: return SqlOperator.LessThanOrEqual;
                case SqlOperator.GreaterThanOrEqual: return SqlOperator.LessThan;
                case SqlOperator.LessThan: return SqlOperator.GreaterThanOrEqual;
                case SqlOperator.LessThanOrEqual: return SqlOperator.GreaterThan;
                case SqlOperator.Add: return SqlOperator.Subtract;
                case SqlOperator.Subtract: return SqlOperator.Add;
                case SqlOperator.Like: return SqlOperator.NotLike;
                case SqlOperator.NotLike: return SqlOperator.Like;
                case SqlOperator.Is: return SqlOperator.IsNot;
                case SqlOperator.IsNot: return SqlOperator.Is;
                case SqlOperator.In: return SqlOperator.NotIn;
                case SqlOperator.NotIn: return SqlOperator.In;
                default: throw new CoPilotUnsupportedException(op.ToString());
            }
        }

        internal static IDecodedNode TransformBooleanReferenceToBinaryExpression(DecodedReference reference, bool value)
        {
            var transformedRef = new DecodedReference(reference.BaseType, reference.Path);
            return new DecodedExpression(ExpressionType.Equal, transformedRef, new DecodedValue(typeof(bool), value));

        }

    }
}
