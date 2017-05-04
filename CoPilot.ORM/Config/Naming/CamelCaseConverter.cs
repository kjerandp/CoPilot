using System.Linq;
using CoPilot.ORM.Extensions;

namespace CoPilot.ORM.Config.Naming
{
    public class CamelCaseConverter : ILetterCaseConverter
    {
        public string Convert(string text)
        {
            return string.Join("", text.Tokenize().Select(TitleCaseWord));
        }

        private static string TitleCaseWord(string word)
        {
            var firstLetter = word[0].ToString().ToUpper();
            if(word.Length == 1) return firstLetter;

            return firstLetter + word.Substring(1).ToLower();


        }
    }
}