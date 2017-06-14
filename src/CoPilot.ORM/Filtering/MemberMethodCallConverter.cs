using System;
using System.Collections.Generic;
using System.Reflection;
using CoPilot.ORM.Common;
using CoPilot.ORM.Database.Providers;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering.Operands;

namespace CoPilot.ORM.Filtering
{
    public delegate void MemberMethodCallConverter(object[] args, ConversionResult result);

    public class MethodCallConverters
    {
        private readonly Dictionary<string, MemberMethodCallConverter> _converters = new Dictionary<string, MemberMethodCallConverter>(10);

        private MethodCallConverters(){}

        public static MethodCallConverters Create(IDbProvider provider)
        {
            var converters = new MethodCallConverters();
            provider.RegisterMethodCallConverters(converters);

            return converters;
        }

        public void Register(string method, MemberMethodCallConverter converter)
        {
            if (_converters.ContainsKey(method))
            {
                _converters[method] = converter;
            }
            else
            {
                _converters.Add(method, converter);
            }
        }

        public void RegisterDefaults()
        {
            Register("StartsWith", StartsWithConverter);
            Register("EndsWith", EndsWithConverter);
            Register("ToLower", ToLowerConverter);
            Register("ToUpper", ToUpperConverter);
            Register("Contains", ContainsConverter);
            Register("Equals", EqualsConverter);
        }

        private static void ContainsConverter(object[] args, ConversionResult result)
        {
            var value = args[0].ToString();

            result.MemberExpressionOperand.WrapWith = "LOWER";
            result.Operator = SqlOperator.Like;
            result.Value = "%" + value.ToLower() + "%";
        }

        private static void ToLowerConverter(object[] args, ConversionResult result)
        {
            result.MemberExpressionOperand.WrapWith = "LOWER";
        }

        private static void ToUpperConverter(object[] args, ConversionResult result)
        {
            result.MemberExpressionOperand.WrapWith = "UPPER";
        }

        private static void StartsWithConverter(object[] args, ConversionResult result)
        {
            var value = args[0].ToString();

            if (args.Length == 2 && args[1].GetType().GetTypeInfo().IsEnum)
            {
                var enumArg = (StringComparison)args[1];

                if (enumArg == StringComparison.CurrentCultureIgnoreCase ||
                    enumArg == StringComparison.OrdinalIgnoreCase)
                {
                    result.MemberExpressionOperand.WrapWith = "UPPER";
                    value = value.ToUpper();
                }
            }
            result.Operator = SqlOperator.Like;
            result.Value = value + "%";
        }

        private static void EndsWithConverter(object[] args, ConversionResult result)
        {
            var value = args[0].ToString();

            if (args.Length == 2 && args[1].GetType().GetTypeInfo().IsEnum)
            {
                var enumArg = (StringComparison)args[1];

                if (enumArg == StringComparison.CurrentCultureIgnoreCase ||
                    enumArg == StringComparison.OrdinalIgnoreCase)
                {
                    result.MemberExpressionOperand.WrapWith = "UPPER";
                    value = value.ToUpper();
                }
            }
            result.Operator = SqlOperator.Like;
            result.Value = "%" + value;
        }

        private static void EqualsConverter(object[] args, ConversionResult result)
        {
            var value = args[0].ToString();

            if (args.Length == 2 && args[1].GetType().GetTypeInfo().IsEnum)
            {
                var enumArg = (StringComparison)args[1];

                if (enumArg == StringComparison.CurrentCultureIgnoreCase ||
                    enumArg == StringComparison.OrdinalIgnoreCase)
                {
                    result.MemberExpressionOperand.WrapWith = "UPPER";
                    value = value.ToUpper();
                }
            }
            result.Operator = SqlOperator.Equal;
            result.Value = value;
        }

        public MemberMethodCallConverter GetConverter(string methodName)
        {
            if (_converters.ContainsKey(methodName))
            {
                return _converters[methodName];
            }
            throw new CoPilotUnsupportedException($"Member method call '{methodName}' not supported!");
        }

    }

    public class ConversionResult
    {
        public ConversionResult(MemberExpressionOperand memberExpressionOperand)
        {
            MemberExpressionOperand = memberExpressionOperand;
        }

        public MemberExpressionOperand MemberExpressionOperand { get; }
        public object Value { get; set; }
        public SqlOperator? Operator { get; set; }
    }
}