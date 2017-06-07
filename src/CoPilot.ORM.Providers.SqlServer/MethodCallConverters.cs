using System;
using System.Collections.Generic;
using System.Reflection;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering;

namespace CoPilot.ORM.Providers.SqlServer
{
    public class MethodCallConverters
    {
        private readonly Dictionary<string, MemberMethodCallConverter> _converters;
        public MethodCallConverters()
        {
            _converters = new Dictionary<string, MemberMethodCallConverter>
            {
                { "StartsWith", StartsWithConverter },
                { "ToLower", ToLowerConverter },
                { "ToUpper", ToUpperConverter },
                { "Contains", ContainsConverter },
                { "ToString", ToStringConverter },
                { "Equals", EqualsConverter }
            };
        }

       

        private static void ToStringConverter(object[] args, ConversionResult result)
        {
            result.MemberExpressionOperand.Custom = "CAST({column} as nvarchar)";
        }

        private static void ContainsConverter(object[] args, ConversionResult result)
        {
            var value = args[0].ToString();

            result.MemberExpressionOperand.WrapWith = "LOWER";
            result.Operator = "LIKE";
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
            result.Operator = "LIKE";
            result.Value = value + "%";
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
            result.Operator = "=";
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

}
