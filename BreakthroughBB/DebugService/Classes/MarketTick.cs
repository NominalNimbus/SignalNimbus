using System;

namespace DebugService.Classes
{
    public class MarketTick : ICloneable
    {
        public int Level { get; set; }
        public decimal BidPrice { get; set; }
        public long BidSize { get; set; }
        public decimal AskPrice { get; set; }
        public long AskSize { get; set; }
        public DateTime Time { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}