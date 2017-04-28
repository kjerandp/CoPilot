using CoPilot.ORM.Filtering.Interfaces;

namespace CoPilot.ORM.Filtering.Operands
{
    public class UnsupportedOperand : IExpressionOperand
    {
        public UnsupportedOperand(string info)
        {
            Info = info;
        }
        public string Info { get; set; }
    }
}