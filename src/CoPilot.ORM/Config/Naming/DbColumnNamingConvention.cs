using System;
using System.Linq;
using CoPilot.ORM.Extensions;

namespace CoPilot.ORM.Config.Naming
{
    public class DbColumnNamingConvention
    {
        public DbColumnNamingConvention()
        {
            PrefixColumnNameWithTableName = true;
            RemoveRepeatedWords = true;
            CaseConverter = new SnakeOrKebabCaseConverter(r => r.ToUpper());
            TableColumnNameSeparator = '_';
        }

        public bool PrefixColumnNameWithTableName { get; set; }
        public bool RemoveRepeatedWords { get; set; }
        public char TableColumnNameSeparator { get; set; }
        public ILetterCaseConverter CaseConverter { get; set; }
        public static DbColumnNamingConvention Default => new DbColumnNamingConvention();

        public static DbColumnNamingConvention SameAsClassMemberNames => new DbColumnNamingConvention
        {
            PrefixColumnNameWithTableName = false,
            CaseConverter = null
        };

        public string Name(string inputName, string tableName = null)
        {
            
            if (PrefixColumnNameWithTableName && RemoveRepeatedWords && tableName != null)
            {
                var a = tableName.Tokenize();
                var b = inputName.Tokenize();

                

                if (a.Last().Equals(b.First(), StringComparison.OrdinalIgnoreCase))
                {
                    if (a.Length > 1)
                    {
                        tableName = tableName.Replace(a.Last(), "");
                    }
                    else
                    {
                        inputName = inputName.Replace(b.First(), "");
                    }
                }
            }

            var n = inputName;
            
            if (CaseConverter != null)
            {
                n = CaseConverter.Convert(inputName);
            }
            if (PrefixColumnNameWithTableName && tableName != null)
            {
                var invalidChars = new[] { '_', ' ', '-' };

                if (invalidChars.Contains(tableName.Last()))
                {
                    tableName = tableName.Substring(0, tableName.Length - 1);
                }
                if (invalidChars.Contains(n.First()))
                {
                    n = n.Substring(1);
                }
                n = tableName + TableColumnNameSeparator + n;
            }
            return n;
        }
    }

}