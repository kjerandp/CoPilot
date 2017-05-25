using CoPilot.ORM.Filtering.Interfaces;

namespace CoPilot.ORM.Filtering.Operands
{
    public class CustomOperand : IExpressionOperand
    {
        private readonly string _stm;
        public CustomOperand(string s)
        {
            _stm = s;
        }

        public override string ToString()
        {
            return _stm;
        }
    }
}