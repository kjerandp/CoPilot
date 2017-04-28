using System;

namespace CoPilot.ORM.Config.DataTypes
{
    public class NumberPrecision
    {
        public NumberPrecision()
        {
            Precision = 18;
            Scale = 0;
        }

        public NumberPrecision(int precision)
        {
            Precision = Math.Min(Math.Max(precision, 0), 38);
            Scale = 0;
        }

        public NumberPrecision(int precision, int scale)
        {
            Precision = Math.Min(Math.Max(precision, 0), 38);
            Scale = Math.Min(Math.Max(scale, 0), precision);
        }
        public int Precision { get; private set; }
        public int Scale { get; private set; }
    }
}
