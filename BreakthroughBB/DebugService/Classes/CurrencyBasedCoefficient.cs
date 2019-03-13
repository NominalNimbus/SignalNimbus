using System;

namespace DebugService.Classes
{
    public class CurrencyBasedCoefficient : ICloneable
    {
        public decimal EUR { get; set; }

        public decimal USD { get; set; }

        public decimal GBP { get; set; }

        public CurrencyBasedCoefficient()
        {
            EUR = 1;
            USD = 1;
            GBP = 1;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}