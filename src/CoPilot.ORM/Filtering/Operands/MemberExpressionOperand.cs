using System;
using CoPilot.ORM.Filtering.Interfaces;

namespace CoPilot.ORM.Filtering.Operands
{
    public class MemberExpressionOperand : IExpressionOperand
    {
        public MemberExpressionOperand(string path)
        {
            Path = path;
        }

        public Type MemberType { get; set; }
        public string Path { get; internal set; }
        public string WrapWith { get; set; }
        public string Custom { get; set; }
        //public string ColumnName { get; internal set; }

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
            return str;
        }

    }
}