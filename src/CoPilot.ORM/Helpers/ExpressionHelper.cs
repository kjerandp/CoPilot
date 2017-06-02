using System;
using System.Linq.Expressions;
using System.Reflection;
using CoPilot.ORM.Config.DataTypes;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering;

namespace CoPilot.ORM.Helpers
{
    public static class ExpressionHelper
    {
        internal static ClassMemberInfo GetMemberInfoFromExpression<T1, T2>(Expression<Func<T1, T2>> expression)
        {
            var memberInfo = GetPropertyFromExpression(expression);
            return ClassMemberInfo.Create(memberInfo);
        }
        internal static string GetPathFromExpression<T>(Expression<T> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpression = expression.Body as UnaryExpression;
                memberExpression = unaryExpression?.Operand as MemberExpression;         
            }
            if (memberExpression == null)
            {
                throw new CoPilotUnsupportedException("Not a valid property reference");
            }
            return PathHelper.RemoveFirstElementFromPathString(memberExpression.ToString());
        }
        internal static MemberInfo GetPropertyFromExpression<T1, T2>(Expression<Func<T1, T2>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpression = expression.Body as UnaryExpression;
                var memberExpr = unaryExpression?.Operand as MemberExpression;
                if (memberExpr != null)
                {
                    return GetPropertyFromMemberExpression<T1>(memberExpr);
                }
                throw new CoPilotUnsupportedException("Not a valid property reference");
            }

            return GetPropertyFromMemberExpression<T1>(memberExpression);
        }
        
        internal static MemberInfo GetPropertyFromMemberExpression<T>(MemberExpression memberExpression)
        {
            var prop = ReflectionHelper.GetMemberType(memberExpression.Member.DeclaringType, memberExpression.Member.Name);

            if (prop?.DeclaringType == null || !prop.DeclaringType.GetTypeInfo().IsAssignableFrom(typeof(T)))
            {
                throw new CoPilotUnsupportedException($"Property is not a property of '{typeof(T).Name}'");
            }
            return prop;
        }
        
        public static ExpressionGraph DecodeExpression<T>(Expression<Func<T,bool>> expression) where T : class
        {
            if (expression == null) return null;
            var decoder = new ExpressionDecoder();
            return decoder.Decode(expression.Body);
        }

        public static bool TryDynamicallyInvokeExpression(Expression expression, out object value)
        {
            try
            {
                value = DynamicallyInvokeExpression(expression);
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        public static object DynamicallyInvokeExpression(Expression expression)
        {
            var value = Expression.Lambda(expression).Compile().DynamicInvoke();

            return value;
        }

        internal static Type FindUnderlyingTypeFromExpression(MemberExpression expression)
        {
            if (expression == null) throw new CoPilotRuntimeException("Expression is null!");
            var exp = expression;
            while (true)
            {
                if (exp.Expression != null)
                {
                    var memberExp = exp.Expression as MemberExpression;
                    if (memberExp == null)
                    {
                        var paramExp = exp.Expression as ParameterExpression;
                        if(paramExp == null) throw new CoPilotRuntimeException("Unable to find underlying type from expression!");
                        return paramExp.Type;
                    }
                    exp = memberExp;
                } else
                {
                    return exp.Type;
                }  
            }
            
        }
    }
}
