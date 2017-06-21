using System;
using System.Text.RegularExpressions;
using CoPilot.ORM.Exceptions;

namespace CoPilot.ORM.Extensions
{
    public static class StringExtensions
    {
        public static string[] Tokenize(this string text)
        {
            if(string.IsNullOrEmpty(text))
                throw new CoPilotRuntimeException("String cannot be empty!");

            const string strRegex = @"((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))";
            const string strReplace = @" $1";
            var regex = new Regex(strRegex);

            text = regex.Replace(text, strReplace).Trim();

            return text.Split(new [] {'_', '-', ' '}, StringSplitOptions.RemoveEmptyEntries);

        }

        public static string RemoveRepeatedNeighboringWords(this string text, char wordSeperator = '_')
        {
            var textSplit = text.Split(wordSeperator);

            if (textSplit.Length > 1)
            {
                var newText = textSplit[0];
                for (var i = 1; i < textSplit.Length; i++)
                {
                    if (!textSplit[i].Equals(textSplit[i - 1], StringComparison.OrdinalIgnoreCase))
                    {
                        newText += wordSeperator + textSplit[i];
                    }
                }
                return newText;
            }
            return text;
        }
    }
}
