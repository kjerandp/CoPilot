using System;
using CoPilot.ORM.Exceptions;
using CoPilot.ORM.Filtering.Decoders.Interfaces;

namespace CoPilot.ORM.Filtering.Decoders.DecodedNodeTypes
{
    public class DecodedValue : IDecodedNode
    {
        public DecodedValue(Type valueType, object value)
        {
            if(value == null) throw new CoPilotUnsupportedException("Value cannot be NULL - use DecodedNullValue");
            ValueType = valueType;
            Value = value;
        }

        public Type ValueType { get; }
        public object Value { get; }
    }
}