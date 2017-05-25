using System;
using CoPilot.ORM.Config.DataTypes;

namespace CoPilot.ORM.Database.Commands
{
    public class DbParameter
    {
        public DbParameter(string name, DbDataType dataType, object defaultValue = null, bool canBeNull = true, bool isOutput = false)
        {
            DefaultValue = defaultValue;
            Name = name;
            DataType = dataType;
            CanBeNull = canBeNull;
            IsOutput = isOutput;
        }

        public string Name { get; }
        public DbDataType DataType { get; private set; }
        public bool CanBeNull { get; private set; }
        public bool IsOutput { get; private set; }
        public object DefaultValue { get; set; }
        public int Size { get; set; }
        public NumberPrecision NumberPrecision { get; set; }

    }
}
