using System;
using System.Linq.Expressions;
using CoPilot.ORM.Config.DataTypes;
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

        public ExpressionGraph Decode(Expression expression)
        {
            _graph = new ExpressionGraph();
            _paramIndex = 1;

            var decoder = ExpressionTypeResolver.Get(expression);
            var root = decoder.Decode();
            var result = ConvertNode(root);
            var binResult = result as BinaryOperand;

            if (binResult == null)
            {
                binResult = ConvertToBinaryOperand(result);
            }

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
                return new BinaryOperand(valOp, valOp, "=");
            }
            
            return new BinaryOperand(valOp, valOp, "!=");
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
                        return new BinaryOperand(op, new NullOperand(), "IS NOT");
                    }
                    if (refNode.ReferencedTypeMemberAccess == "Date")
                    {
                        op.Custom = "CONVERT(date, {column})";
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Unsupported member access on referenced type! ({refNode.ReferencedTypeMemberAccess})");
                    }
                }
                if (!string.IsNullOrEmpty(refNode.ReferencedTypeMethodCall))
                {
                    var converter = ExpressionDecoderConfig.GetConverter(refNode.ReferencedTypeMethodCall);
                    var result = new ConversionResult(op);
                    converter.Invoke(refNode.ReferenceTypeMethodCallArgs, result);

                    if (result.Value != null && !string.IsNullOrEmpty(result.Operator))
                    {
                        var valueOpterand = new ValueOperand($"@param{_paramIndex++}", result.Value);
                        return new BinaryOperand(result.MemberExpressionOperand, valueOpterand, result.Operator);
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

                var leftConst = binaryNode.Left as DecodedNullValue;
                var rightConst = binaryNode.Right as DecodedNullValue;

                if (leftConst != null || rightConst != null)
                {
                    binOp.Operator = "IS";
                    if (binaryNode.Operand == ExpressionType.NotEqual)
                    {
                        binOp.Operator += " NOT";
                    }
                }

                if (binaryNode.Operand == ExpressionType.OrElse)
                {
                    binOp.Enclose = true;
                }
                
                return binOp;
            }
            throw new ApplicationException("Unable to decode " + node.GetType().AssemblyQualifiedName);
        }

        
        private static string Translate(ExpressionType expressionType)
        {

            switch (expressionType)
            {
                case ExpressionType.AndAlso: return "AND";
                case ExpressionType.OrElse: return "OR";
                case ExpressionType.Equal: return "=";
                case ExpressionType.NotEqual: return "<>";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.Add: return "+";
                case ExpressionType.Subtract: return "-";
                default: throw new NotSupportedException(expressionType.ToString());
            }
        }

        
        
    }
}
