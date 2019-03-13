using System;

namespace DebugService.Classes
{
    public class Bar : ICloneable
    {
        public DateTime Timestamp { get; set; }

        public decimal OpenBid { get; set; }

        public decimal OpenAsk { get; set; }

        public decimal Open => OpenBid == 0M ? OpenAsk : (OpenAsk == 0M ? OpenBid : ((OpenBid + OpenAsk) / 2M));

        public decimal HighBid { get; set; }

        public decimal HighAsk { get; set; }

        public decimal High => HighBid == 0M ? HighAsk : (HighAsk == 0M ? HighBid : ((HighBid + HighAsk) / 2M));

        public decimal LowBid { get; set; }

        public decimal LowAsk { get; set; }

        public decimal Low => LowBid == 0M ? LowAsk : (LowAsk == 0M ? LowBid : ((LowBid + LowAsk) / 2M));

        public decimal CloseBid { get; set; }

        public decimal CloseAsk { get; set; }

        public decimal MeanClose => CloseBid == 0M ? CloseAsk : (CloseAsk == 0M ? CloseBid : ((CloseBid + CloseAsk) / 2M));

        public decimal PriceVar { get; set; }

        public long VolumeBid { get; set; }

        public long VolumeAsk { get; set; }

        public long MeanVolume => VolumeBid == 0.0 ? VolumeAsk : (VolumeAsk == 0.0 ? VolumeBid : (long)((VolumeBid + VolumeAsk) / 2.0));

        public long VolumeVar { get; set; }

        public Bar()
        {
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}