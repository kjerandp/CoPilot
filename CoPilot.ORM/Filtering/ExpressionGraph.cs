using CoPilot.ORM.Filtering.Operands;

namespace CoPilot.ORM.Filtering
{
    public class ExpressionGraph
    {
        public BinaryOperand Root { get; set; }

        
        public override string ToString()
        {
            return Root.ToString();
        }


        
    }
}
