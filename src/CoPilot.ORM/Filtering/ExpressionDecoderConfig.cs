using System;
using System.Collections.Generic;
using CoPilot.ORM.Filtering.Operands;
using System.Reflection;
using CoPilot.ORM.Exceptions;

namespace CoPilot.ORM.Filtering
{
    public static class ExpressionDecoderConfig
    {
        public delegate void MemberMethodCallConverter(object[] args, ConversionResult result);

        private static readonly Dictionary<string, MemberMethodCallConverter> Converters = new Dictionary<string, MemberMethodCallConverter>();

        static ExpressionDecoderConfig()
        {
            RegisterDefaultConverters();
        }

        private static void RegisterDefaultConverters()
        {
            AddConverter("StartsWith", StartsWithConverter);
            AddConverter("ToLower", ToLowerConverter);
            AddConverter("ToUpper", ToUpperConverter);
            AddConverter("Contains", ContainsConverter);
            AddConverter("ToString", ToStringConverter);
            AddConverter("Equals", EqualsConverter);
            //DateTime.Date?
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

        public static void AddConverter(string methodName, MemberMethodCallConverter converter)
        {
            if (!Converters.ContainsKey(methodName))
                Converters.Add(methodName, null);

            Converters[methodName] = converter;
        }

        public static MemberMethodCallConverter GetConverter(string methodName)
        {
            if (Converters.ContainsKey(methodName))
            {
                return Converters[methodName];
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
        public string Operator { get; set; }
    }
}