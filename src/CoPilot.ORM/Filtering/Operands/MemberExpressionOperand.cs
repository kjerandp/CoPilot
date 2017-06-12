using System;
using CoPilot.ORM.Context;
using CoPilot.ORM.Filtering.Interfaces;

namespace CoPilot.ORM.Filtering.Operands
{
    public class MemberExpressionOperand : IExpressionOperand
    {
        public MemberExpressionOperand(string path)
        {
            Path = path;
        }

        public MemberExpressionOperand(ContextColumn columnReference)
        {
            ColumnReference = columnReference;
        }

        public string Path { get; internal set; }
        public string WrapWith { get; set; }
        public string Custom { get; set; }
        public ContextColumn ColumnReference { get; internal set; }

        public override string ToString()
        {
            var str = "{column}";
            if (!string.IsNullOrEmpty(Custom))
            {
                return Custom.Replace("{column}", str);
            } 
            if (!string.IsNullOrEmpty(WrapWith))
            {
                str = $"{WrapWith}({str})";
            }

            return ColumnReference != null ? str.Replace("{column}", $"T{ColumnReference.Node.Index}.{ColumnReference.Column.ColumnName}"):str;

            
        }

    }
}