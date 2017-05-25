using CoPilot.ORM.Filtering.Interfaces;

namespace CoPilot.ORM.Filtering.Operands
{
    public class ValueOperand : IExpressionOperand
    {
        public ValueOperand(string paramName, object value)
        {
            ParamName = paramName;
            Value = value;
        }
        public object Value { get; internal set; }
        public string ParamName { get; set; }

        public override string ToString()
        {
            return ParamName;
        }    
    }

    public class ValueListOperand : ValueOperand
    {
        public ValueListOperand(string paramName, object value) : base(paramName, value){}
        
        public override string ToString()
        {
            return "("+ParamName+")";
        }
    }
}