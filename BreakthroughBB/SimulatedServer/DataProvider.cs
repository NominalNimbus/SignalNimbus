using System;
using System.Collections.Generic;
using System.Linq;
using CommonObjects;
using DebugService.Classes;
using UserCode;
using Bar = CommonObjects.Bar;
using Periodicity = CommonObjects.Timeframe;

namespace SimulatedServer
{
    public class DataProvider : IDataProvider
    {
        private readonly List<string> _availableDataFeeds;
        private readonly List<HistoricalData> _historicalData;
        private readonly Dictionary<HistoricalData, Quote> _lastTicks;

        public event EventHandler<HistoricalData> NewBarAppended;

        public DataProvider(IEnumerable<HistoricalData> historicalData)
        {
            _historicalData = new List<HistoricalData>(historicalData);
            _availableDataFeeds = new List<string>(_historicalData.Select(p => p.DataFeed).Distinct());
            _lastTicks = new Dictionary<HistoricalData, Quote>();
        }

        public void Dispose()
        {
        }

        public List<string> AvailableSymbolsForDataFeed(string dataFeedName)
        {
            return _historicalData.Where(p => p.DataFeed.Equals(dataFeedName)).Select(p => p.Symbol).ToList();
        }

        public List<Bar> GetBars(Selection parameters)
        {
            DebugService.Classes.Periodicity period;
            switch (parameters.Timeframe)
            {
                case Periodicity.Hour: period = DebugService.Classes.Periodicity.Hour; break;
                case Periodicity.Day: period = DebugService.Classes.Periodicity.Day; break;
                case Periodicity.Month: period = DebugService.Classes.Periodicity.Month; break;
                default: period = DebugService.Classes.Periodicity.Minute; break;
            }

            var history = _historicalData
                .FirstOrDefault(p => p.DataFeed.Equals(parameters.DataFeed) 
                    && p.Symbol.Equals(parameters.Symbol) 
                    && p.Periodicity == period 
                    && p.Interval == parameters.TimeFactor);

            if (history == null || !history.Bars.Any())
                return new List<Bar>();

            int barsToSkip = history.Bars.Count > parameters.BarCount
                ? history.Bars.Count - parameters.BarCount
                : 0;
            return history.Bars.OrderBy(b => b.Timestamp).Skip(barsToSkip).Select(ToBar).ToList();
        }

        public List<Bar> GetBars(Selection parameters, DateTime from, DateTime to)
        {
            if (from == DateTime.MinValue && (to == DateTime.MinValue || to == DateTime.MaxValue))
                return GetBars(parameters);

            DebugService.Classes.Periodicity period;
            switch (parameters.Timeframe)
            {
                case Periodicity.Hour: period = DebugService.Classes.Periodicity.Hour; break;
                case Periodicity.Day: period = DebugService.Classes.Periodicity.Day; break;
                case Periodicity.Month: period = DebugService.Classes.Periodicity.Month; break;
                default: period = DebugService.Classes.Periodicity.Minute; break;
            }

            var history = _historicalData
                .FirstOrDefault(p => p.DataFeed.Equals(parameters.DataFeed)
                    && p.Symbol.Equals(parameters.Symbol)
                    && p.Periodicity == period
                    && p.Interval == parameters.TimeFactor);

            if (history == null || !history.Bars.Any())
                return new List<Bar>();

            if (to <= from)
                to = DateTime.MaxValue;

            return history.Bars
                .Where(b => b.Timestamp >= from && b.Timestamp <= to)
                .Select(ToBar).OrderBy(b => b.Date).ToList();
        }

        public Tick GetTick(string dataFeed, string symbol, DateTime timestamp)
        {
            var last = GetLastTick(dataFeed, symbol);
            if (timestamp == DateTime.MinValue || (last != null && last.Date <= timestamp))
                return last;

            var bars = GetBars(new Selection
            {
                BarCount = 2,
                DataFeed = dataFeed,
                Symbol = symbol,
                TimeFactor = 1,
                Timeframe = Periodicity.Minute
            });
            if (bars != null && bars.Count != 0)
            {
                var bar = bars[bars.Count - 1];
                return new Tick
                {
                    Symbol = new Security { Symbol = symbol, DataFeed = dataFeed },
                    Price = bar.MeanClose,
                    Date = bar.Date,
                    Bid = bar.MeanClose - bar.MeanClose * 0.05M,
                    Ask = bar.MeanClose + bar.MeanClose * 0.05M
                };
            }

            return null;
        }

        public Tick GetLastTick(string dataFeed, string symbol)
        {
            lock (_lastTicks)
            {
                var hd = _historicalData.FirstOrDefault(p => p.DataFeed == dataFeed && p.Symbol == symbol);

                if (hd == null || !_lastTicks.ContainsKey(hd))
                    return null;

                return new Tick
                {
                    Ask = _lastTicks[hd].AskPrice,
                    AskSize = _lastTicks[hd].AskSize,
                    Bid = _lastTicks[hd].BidPrice,
                    BidSize = _lastTicks[hd].BidSize,
                    DataFeed = dataFeed,
                    Symbol = new Security
                    {
                        Symbol = symbol,
                        DataFeed = dataFeed,
                        Name = symbol,
                        SecurityId = hd.SecurityID
                    },
                    Volume = _lastTicks[hd].Volume,
                    Date = _lastTicks[hd].Time,
                    Price = (_lastTicks[hd].AskPrice + _lastTicks[hd].BidPrice) / 2,
                    Level2 = new List<MarketLevel2>(_lastTicks[hd].Level2.Select(p => new MarketLevel2
                    {
                        AskPrice = p.AskPrice,
                        BidPrice = p.BidPrice,
                        AskSize = p.AskSize,
                        BidSize = p.BidSize,
                        DomLevel = p.Level
                    }))
                       
                };
            }
        }

        public string GetLastError()
        {
            return String.Empty;
        }

        public List<string> AvailableDataFeeds
        {
            get { return _availableDataFeeds.ToList(); }
        }

        public void AppendTick(Quote tick, HistoricalData data)
        {
            lock (_historicalData)
            {
                var hd = _historicalData.FirstOrDefault(p => p.Equals(data));
                if(hd == null)
                    return;

                InternalAppendTick(tick, hd);

                lock (_lastTicks)
                {
                    if (!_lastTicks.ContainsKey(hd))
                        _lastTicks.Add(hd, tick);
                    else
                        _lastTicks[hd] = tick;
                }
            }
          
        }

        private void InternalAppendTick(Quote tick, HistoricalData data)
        {
            var timeSpan = new TimeSpan();

            if (data.Periodicity == DebugService.Classes.Periodicity.Minute)
                timeSpan = TimeSpan.FromMinutes(data.Interval);
            if (data.Periodicity == DebugService.Classes.Periodicity.Hour)
                timeSpan = TimeSpan.FromHours(data.Interval);
            if (data.Periodicity == DebugService.Classes.Periodicity.Day)
                timeSpan = TimeSpan.FromDays(data.Interval);
            if (data.Periodicity == DebugService.Classes.Periodicity.Month)
                timeSpan = TimeSpan.FromDays(data.Interval * 30);
           
            var lastBar = data.Bars.Last();

            if(lastBar == null)
                return;
            
            if ((tick.Time - lastBar.Timestamp) >= timeSpan)
            {
                data.Bars.Add(new DebugService.Classes.Bar
                {
                    Timestamp = lastBar.Timestamp + timeSpan,
                    OpenBid = tick.BidPrice,
                    OpenAsk = tick.AskPrice,
                    HighBid = tick.BidPrice,
                    HighAsk = tick.AskPrice,
                    LowBid = tick.BidPrice,
                    LowAsk = tick.AskPrice,
                    CloseBid = tick.BidPrice,
                    CloseAsk = tick.AskPrice,
                    VolumeBid = (long)tick.BidSize,
                    VolumeAsk = (long)tick.AskSize
                });

                NewBarAppended?.Invoke(this, data);
            }
            else
            {
                if (lastBar.HighBid < tick.BidPrice)
                    lastBar.HighBid = tick.BidPrice;
                if (lastBar.HighAsk < tick.AskPrice)
                    lastBar.HighAsk = tick.AskPrice;
                if (lastBar.LowBid > tick.BidPrice)
                    lastBar.LowBid = tick.BidPrice;
                if (lastBar.LowAsk > tick.AskPrice)
                    lastBar.LowAsk = tick.AskPrice;

                lastBar.CloseBid = tick.BidPrice;
                lastBar.CloseAsk = tick.AskPrice;
                lastBar.VolumeBid += (long)tick.BidSize;
                lastBar.VolumeAsk += (long)tick.AskSize;
            }
        }

        private static Bar ToBar(DebugService.Classes.Bar bar)
        {
            return new Bar
            {
                Date = bar.Timestamp,
                OpenBid = bar.OpenBid,
                OpenAsk = bar.OpenAsk,
                HighBid = bar.HighBid,
                HighAsk = bar.HighAsk,
                LowBid = bar.LowBid,
                LowAsk = bar.LowAsk,
                CloseBid = bar.CloseBid,
                CloseAsk = bar.CloseAsk,
                VolumeBid = bar.VolumeBid,
                VolumeAsk = bar.VolumeAsk
            };
        }
    }
}