using System;
using System.Linq.Expressions;
using CoPilot.ORM.Filtering.Decoders.Interfaces;

namespace CoPilot.ORM.Filtering.Decoders
{
    public static class ExpressionTypeResolver
    {
        public static IExpressionDecoder Get(Expression expression)
        {
            var binaryExpression = expression as BinaryExpression;
            if(binaryExpression != null) return new BinaryExpressionDecoder(binaryExpression);
            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null) return new ConstantExpressionDecoder(constantExpression);
            var memberExpression = expression as MemberExpression;
            if (memberExpression != null) return new MemberExpressionDecoder(memberExpression);
            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null) return new UnaryExpressionDecoder(unaryExpression);
            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null) return new MethodCallExpressionDecoder(methodCallExpression);

            throw new ArgumentException($"Expression not recognized! {expression.GetType().AssemblyQualifiedName}");
        }
    }
}