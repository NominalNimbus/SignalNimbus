using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DebugService.Classes;

namespace DebugService
{
    public static class GlobalHelper
    {
        /// <summary>
        /// Read text from file and parse this text/.
        /// </summary>
        /// <param name="fileName">File destination path</param>
        public static HistoricalData LoadDataFromCSV(string fileName)
        {
            const NumberStyles whatever = NumberStyles.Any;
            CultureInfo invariant = CultureInfo.InvariantCulture;

            var text = File.ReadAllLines(fileName).ToList();

            if (text.Count < 2)
                return null;

            var res = new HistoricalData();
            var desc = text[0].Split(',');
            text.RemoveAt(0);

            if(desc.Length < 5)
                return null;

            res.Symbol = desc[0];
            res.DataFeed = desc[2];

            Periodicity periodicity;
            if (!Periodicity.TryParse(desc[3], false, out periodicity))
                return null;

            res.Periodicity = periodicity;

            int interval;
            if (!int.TryParse(desc[4], out interval))
                return null;

            res.Interval = interval;

             int security;
             if (!int.TryParse(desc[1], out security))
                return null;

            res.SecurityID = security;

            int slot;
            if (desc.Length > 5 && int.TryParse(desc[5], out slot))
                res.Slot = slot;

            for (int i = 0; i < text.Count; i++)
            {
                var line = text[i];
                var data = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (data.Length == 1 && data[0].StartsWith("Ticks"))
                {
                    var item = text.GetRange(i + 1, text.Count - i - 1).FirstOrDefault(p => p.StartsWith("Ticks"));
                    var index = text.IndexOf(item, i + 1);
                    if (index == -1)
                        index = text.Count;

                    var quotes = ParseTicks(text, i + 1, index);

                    i = index - 1;

                    if (quotes.Count == 0)
                        continue;

                    if (res.Quotes.Count == 0)
                    {
                        foreach (var quote in quotes.Where(p => p.Time != DateTime.MinValue))
                            res.Quotes.Add(ToQuote(quote));
                    }
                    else
                    {
                        for (var j = 0; j < res.Quotes.Count; j++)
                        {
                            if (quotes.Count <= j)
                                break;

                            var level2 = quotes[j];
                            level2.Level = res.Quotes[j].Level2.Count + 1;
                            res.Quotes[j].Level2.Add(level2);
                        }
                    }
                }
                else if (data.Length == 6)
                {
                    DateTime time;
                    decimal open, high, low, close;
                    long volume;
                    if (Decimal.TryParse(data[0], whatever, invariant, out open)
                        && Decimal.TryParse(data[1], whatever, invariant, out high)
                        && Decimal.TryParse(data[2], whatever, invariant, out low)
                        && Decimal.TryParse(data[3], whatever, invariant, out close)
                        && Int64.TryParse(data[4], whatever, invariant, out volume)
                        && DateTime.TryParse(data[5], out time))
                    {
                        res.Bars.Add(new Bar
                        {
                            Timestamp = time,
                            OpenBid = open,
                            OpenAsk = open,
                            HighBid = high,
                            HighAsk = high,
                            LowBid = low,
                            LowAsk = low,
                            CloseBid = close,
                            CloseAsk = close,
                            VolumeBid = volume,
                            VolumeAsk = volume
                        });
                    }
                }
                else if (data.Length == 11)
                {
                    DateTime time;
                    decimal openB, openA, highB, highA, lowB, lowA, closeB, closeA;
                    long volumeB, volumeA;
                    if (Decimal.TryParse(data[0], whatever, invariant, out openB)
                        && Decimal.TryParse(data[1], whatever, invariant, out openA)
                        && Decimal.TryParse(data[2], whatever, invariant, out highB)
                        && Decimal.TryParse(data[3], whatever, invariant, out highA)
                        && Decimal.TryParse(data[4], whatever, invariant, out lowB)
                        && Decimal.TryParse(data[5], whatever, invariant, out lowA)
                        && Decimal.TryParse(data[6], whatever, invariant, out closeB)
                        && Decimal.TryParse(data[7], whatever, invariant, out closeA)
                        && Int64.TryParse(data[8], whatever, invariant, out volumeB)
                        && Int64.TryParse(data[9], whatever, invariant, out volumeA)
                        && DateTime.TryParse(data[10], out time))
                    {
                        res.Bars.Add(new Bar
                        {
                            Timestamp = time,
                            OpenBid = openB,
                            OpenAsk = openA,
                            HighBid = highB,
                            HighAsk = highA,
                            LowBid = lowB,
                            LowAsk = lowA,
                            CloseBid = closeB,
                            CloseAsk = closeA,
                            VolumeBid = volumeB,
                            VolumeAsk = volumeA
                        });
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Create simulated historical data for code running, according to input parameters
        /// </summary>
        public static HistoricalData CreateSimulated(SimulatedDataParameters parameters)
        {
            var res = new HistoricalData
            {
                Symbol = parameters.Symbol,
                DataFeed = parameters.DataFeed,
                Periodicity = parameters.Periodicity,
                Interval = parameters.Interval,
                SecurityID = parameters.SecurityId,
                Slot = parameters.Slot
            };

            var timeSpan = new TimeSpan();
            var barDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);
            var quoteInterval = new TimeSpan();

            if (parameters.Periodicity == Periodicity.Minute)
            {
                timeSpan = TimeSpan.FromMinutes(parameters.Interval);
                quoteInterval = TimeSpan.FromMinutes(parameters.Interval / 5.0);
                barDate = barDate.AddMinutes((parameters.Interval*parameters.BarsCount)*-2);
            }
            if (parameters.Periodicity == Periodicity.Hour)
            {
                timeSpan = TimeSpan.FromHours(parameters.Interval);
                quoteInterval = TimeSpan.FromHours(parameters.Interval / 5.0);
                barDate = barDate.AddHours((parameters.Interval * parameters.BarsCount) * -2);
            }
            if (parameters.Periodicity == Periodicity.Day)
            {
                timeSpan = TimeSpan.FromDays(parameters.Interval);
                quoteInterval = TimeSpan.FromDays(parameters.Interval / 5.0);
                barDate = barDate.AddDays((parameters.Interval * parameters.BarsCount) * -2);
            }
            if (parameters.Periodicity == Periodicity.Month)
            {
                timeSpan = TimeSpan.FromDays(parameters.Interval*30);
                quoteInterval = TimeSpan.FromDays(parameters.Interval * 30 / 5.0);
                barDate = barDate.AddMonths((parameters.Interval * parameters.BarsCount) * -2);
            }

            var random = new Random(Guid.NewGuid().GetHashCode());
            var lastBid = random.Next((int) (parameters.PriceMin*1000), (int) (parameters.PriceMax*1000))/1000M;
            for (var i = 0; i < parameters.BarsCount; i++)
            {
                decimal pctSpread = Math.Max(2, i % 10) / 1000M;
                barDate = barDate + timeSpan;
                var priceBid = random.Next((int)(parameters.PriceMin * 1000), (int)(parameters.PriceMax * 1000)) / 1000M;
                var price1 = random.Next((int)(parameters.PriceMin * 1000), (int)(parameters.PriceMax * 1000)) / 1000M;
                var price2 = random.Next((int)(parameters.PriceMin * 1000), (int)(parameters.PriceMax * 1000)) / 1000M;
                var lastAsk = lastBid + lastBid * pctSpread;
                var priceAsk = priceBid + priceBid * pctSpread;
                res.Bars.Add(new Bar
                {
                    Timestamp = barDate,
                    OpenBid = lastBid,
                    OpenAsk = lastAsk,
                    CloseBid = priceBid,
                    CloseAsk = priceAsk,
                    HighBid = Math.Max(Math.Max(priceBid, lastBid), Math.Max(price1, price2)),
                    HighAsk = Math.Max(Math.Max(priceAsk, lastAsk), Math.Max(price1, price2)),
                    LowBid = Math.Min(Math.Min(priceBid, lastBid), Math.Min(price1, price2)),
                    LowAsk = Math.Min(Math.Min(priceAsk, lastAsk), Math.Min(price1, price2)),
                    VolumeBid = new Random(Guid.NewGuid().GetHashCode()).Next(10000, 200000),
                    VolumeAsk = new Random(Guid.NewGuid().GetHashCode()).Next(10000, 200000)
                });

                lastBid = priceBid;
            }

            for (var i = 0; i < parameters.TicksCount; i++)
            {
                barDate = barDate + quoteInterval;
                var price = random.Next((int)(parameters.PriceMin * 1000), (int)(parameters.PriceMax * 1000)) / 1000.0;
                var price1 = random.Next((int)(parameters.PriceMin * 1000), (int)(parameters.PriceMax * 1000)) / 1000.0;
                var bidSize = new Random(Guid.NewGuid().GetHashCode()).Next(1000, 20000);
                var askSize = new Random(Guid.NewGuid().GetHashCode()).Next(1000, 20000);

                res.Quotes.Add(new Quote
                {
                    BidPrice = (decimal)Math.Max(price, price1),
                    AskPrice = (decimal)Math.Min(price, price1),
                    BidSize = bidSize,
                    AskSize = askSize,
                    Volume = bidSize + askSize,
                    Time = barDate
                });

                for (var j = 1; j <= parameters.MarketLevels; j++)
                {
                    var priceL = random.Next((int)(parameters.PriceMin * 1000), (int)(parameters.PriceMax * 1000)) / 1000.0;
                    var price1L = random.Next((int)(parameters.PriceMin * 1000), (int)(parameters.PriceMax * 1000)) / 1000.0;

                    res.Quotes.Last().Level2.Add(new MarketTick
                    {
                        BidPrice = (decimal)Math.Max(priceL, price1L),
                        AskPrice = (decimal)Math.Min(priceL, price1L),
                        BidSize = bidSize / j + 1 ,
                        AskSize = askSize / j + 1,
                    });
                }
            }

            return res;
        }

        /// <summary>
        /// Parse one level of ticks
        /// </summary>
        /// <param name="text">All text from CSV file</param>
        /// <param name="start">Start position for parsing</param>
        /// <param name="end">Start end for parsing</param>
        /// <returns>List of ticks</returns>
        private static List<MarketTick> ParseTicks(List<string> text, int start, int end)
        {
            var res = new List<MarketTick>();

            for (var i = start; i <= end; i++)
            {
                if (i >= text.Count)
                    return res;

                var line = text[i];
                var data = line.Split(',');
                if (data.Length >= 4)
                {
                    decimal bid;
                    decimal ask;
                    long bidSize;
                    long askSize;

                    if (!decimal.TryParse(data[0], NumberStyles.Any, CultureInfo.InvariantCulture, out bid)     ||
                        !decimal.TryParse(data[2], NumberStyles.Any, CultureInfo.InvariantCulture, out ask)     ||
                        !long.TryParse(data[1], NumberStyles.Any, CultureInfo.InvariantCulture, out bidSize) ||
                        !long.TryParse(data[3], NumberStyles.Any, CultureInfo.InvariantCulture, out askSize))
                        continue;

                    res.Add(new MarketTick
                    {
                        BidPrice = bid,
                        AskPrice = ask,
                        BidSize = bidSize,
                        AskSize = askSize
                    });
                }

                if (data.Length == 5)
                {
                    DateTime time;
                    if (!DateTime.TryParse(data[4], out time))
                        res.Last().Time = DateTime.MinValue;
                    else
                        res.Last().Time = time;
                }
            }

            return res;
        }

        /// <summary>
        /// Convert tick data to Level 1 Quote
        /// </summary>
        private static Quote ToQuote(MarketTick tick)
        {
            var volume = tick.AskSize + tick.BidSize;
            return new Quote
            {
                AskPrice = tick.AskPrice,
                BidPrice = tick.BidPrice,
                AskSize = tick.AskSize,
                BidSize = tick.BidSize,
                Volume = volume,
                Level2 = new List<MarketTick>(),
                Time = tick.Time
            };
        }

        /// <summary>
        /// Creating string for writing in .CSV file
        /// </summary>
        /// <param name="data">Simulated historical data</param>
        public static string CreateCSV(HistoricalData data)
        {
            var builder = new StringBuilder();

            builder.AppendFormat("{0},{1},{2},{3},{4},{5}{6}", data.Symbol, data.SecurityID, data.DataFeed, data.Periodicity, data.Interval, data.Slot, Environment.NewLine);

            foreach (var bar in data.Bars)
                builder.AppendFormat("{0},{1},{2},{3},{4},{5}{6}", bar.Open, bar.High, bar.Low, bar.MeanClose, bar.MeanVolume, bar.Timestamp.ToString("MM/dd/yyyy hh:mm"), Environment.NewLine);

            if (data.Quotes.Count == 0)
                return builder.ToString();

            builder.Append("Ticks" + Environment.NewLine);

            foreach (var quote in data.Quotes)
                builder.AppendFormat("{0},{1},{2},{3},{4}{5}", quote.BidPrice, quote.BidSize, quote.AskPrice, quote.AskSize, quote.Time.ToString("MM/dd/yyyy hh:mm:ss"), Environment.NewLine);

            var maxCount = data.Quotes.Max(p => p.Level2.Count);

            if(maxCount == 0)
                return builder.ToString();

            for (var i = 0; i < maxCount - 1; i++)
            {
                builder.Append("Ticks" + Environment.NewLine);

                foreach (var quote in data.Quotes)
                {
                    if(quote.Level2.Count <= i)
                        continue;

                    builder.AppendFormat("{0},{1},{2},{3}{4}", quote.Level2[i].BidPrice, quote.Level2[i].BidSize, quote.Level2[i].AskPrice, quote.Level2[i].AskSize, Environment.NewLine);
                }
            }

            return builder.ToString();
        }
    }
}

