using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace DebugService.Classes
{
    public class Quote : ICloneable
    {
        public Quote()
        {
            AskPrice = 0;
            BidPrice = 0;
            Volume = 0;
            AskSize = 0;
            BidSize = 0;
            Level2 = new List<MarketTick>();
            Time = DateTime.UtcNow;
        }

        public decimal AskPrice { get; set; }
        public decimal AskSize { get; set; }
        public decimal BidPrice { get; set; }
        public decimal BidSize { get; set; }
        public long Volume { get; set; }
        public DateTime Time { get; set; }

        [XmlArray("Level2"), XmlArrayItem("MarketTick", typeof(MarketTick))]
        public List<MarketTick> Level2 { get; set; }
        
        public object Clone()
        {
            var clone = MemberwiseClone() as Quote;
            clone.Level2 = new List<MarketTick>(Level2.Select(p => p.Clone() as MarketTick));
            return clone;
        }
    }
}