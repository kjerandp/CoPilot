using System;
using CoPilot.ORM.Extensions;

namespace CoPilot.ORM.Config.Naming
{
    public class SnakeOrKebabCaseConverter : ILetterCaseConverter
    {
        private readonly char _seperator;
        private readonly Func<string, string> _postFunc;

        public SnakeOrKebabCaseConverter() : this('_', null) { }

        public SnakeOrKebabCaseConverter(char seperator) : this(seperator, null){}

        public SnakeOrKebabCaseConverter(Func<string, string> postFunc) : this('_', postFunc){}

        public SnakeOrKebabCaseConverter(char seperator, Func<string, string> postFunc)
        {
            _seperator = seperator;
            _postFunc = postFunc;
        }

        public string Convert(string text)
        {
            var str = string.Join(_seperator.ToString(), text.Tokenize());
            return _postFunc != null ? _postFunc.Invoke(str) : str;
        }
    }
}