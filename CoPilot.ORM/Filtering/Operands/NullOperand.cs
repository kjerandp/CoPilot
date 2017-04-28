using CoPilot.ORM.Filtering.Interfaces;

namespace CoPilot.ORM.Filtering.Operands
{
    public class NullOperand : IExpressionOperand
    {
        public override string ToString()
        {
            return "NULL";
        }
    }
}