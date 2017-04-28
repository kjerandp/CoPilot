using System;
using CoPilot.ORM.Filtering.Decoders.Interfaces;

namespace CoPilot.ORM.Filtering.Decoders.DecodedNodeTypes
{
    public class DecodedValue : IDecodedNode
    {
        public DecodedValue(Type valueType, object value)
        {
            if(value == null) throw new ArgumentException("Value cannot be NULL - use DecodedNullValue");
            ValueType = valueType;
            Value = value;
        }

        public Type ValueType { get; }
        public object Value { get; }
    }
}