using CoPilot.ORM.Database.Commands;
using CoPilot.ORM.Filtering.Interfaces;
using CoPilot.ORM.Helpers;

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

        public DbParameter GetParameter()
        {
            return new DbParameter(ParamName, DbConversionHelper.MapToDbDataType(Value?.GetType()));
        }

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