using System;
using System.Collections.Generic;
using System.Linq;
using CommonObjects;
using UserCode;
using Auxiliaries;
using UserCode.TI;

namespace TestManagment
{
    /// TestManagment signal
    public class BreakthroughBB : SignalBase
    {
        private bool _internalBacktest;
        public StartMethod _startMethod;
        private ExecuteTradesParam _execTradesParam;
        private PriceConstants backtestPriceConst;
        private int _backtestBatch;
        public int _tradeSlot { get; private set; }
        public int _tradingPeriod { get; private set; }
        public Dictionary<Selection, IEnumerable<Bar>> _instrumentData { get; private set; }

        public BreakthroughBB()
        {
            Name = "TestManagment";
            _execTradesParam = new ExecuteTradesParam();
            _instrumentData = new Dictionary<Selection, IEnumerable<Bar>>();
        }

        /// <summary>
        /// Initializes signal instruments
        /// </summary>
        /// <param name="selections">List of data descriptions on which code will be run</param>
        /// <returns>True if succeeded</returns>
        protected override bool InternalInit(IEnumerable<Selection> selections)
        {
            Selections.Clear();
            Selections.AddRange(selections);
            StartMethod = _startMethod;
            if (StartMethod == StartMethod.Periodic)
                ExecutionPeriod = _tradingPeriod;

            _execTradesParam.TradeableSymbols = Selections
                .Where(i => i.MarketDataSlot == _tradeSlot).Select(i => i.Symbol).Distinct().ToList();
            _execTradesParam.DataFeed = DataProvider;

            // Your code initialization

            return true;
        }

        /// <summary>
        /// Runs on new quote, new bar or by timer (see <see cref="StartMethod"/> property)
        /// </summary>
        /// <param name="instrument">Instrument that triggered execution (optional)</param>
        /// <param name="ticks">Quotes collected since previous execution (optional)</param>
        protected override void InternalStart(Selection instrument = null, IEnumerable<Tick> ticks = null)
        {
            var signalParam = OrigParameters;
            foreach (var item in Selections)
                _instrumentData.Add(item, DataProvider.GetBars(item));

            var trades = Evaluate(_instrumentData, signalParam, instrument, ticks);
            TradeSignal(trades);

            if (trades != null && trades.Count > 0)
            {
                // Perform some action based on the trade details
                //ExecuteTrades.Execute(trades, this, _execTradesParam);
                foreach (var account in BrokerAccounts)
                {
                    foreach (var orderInfo in GenerateOrderParams(trades))
                    {
                        PlaceOrder(orderInfo, account);
                    }
                }
            }

            _instrumentData.Clear();
        }

        /// <summary>
        /// Runs backtest for single instrument and a set of parameter values
        /// </summary>
        /// <param name="instruments">Instruments to be backtested</param>
        /// <param name="parameters">Set of parameter values to use for backtest</param>
        /// <returns>List of generated trades</returns>
        protected override List<TradeSignal> BacktestSlotItem(IEnumerable<Selection> instruments,
            IEnumerable<object> parameters)
        {
            // Get data for all provided instruments
            var data = new Dictionary<Selection, List<Bar>>(instruments.Count());

            if (BacktestSettings == null) BacktestSettings = new BacktestSettings();

            if (BacktestSettings?.BarData != null && BacktestSettings.BarData.Any()) //provided with backtest settings
            {
                foreach (var item in instruments)
                {
                    var bars = BacktestSettings.BarData
                        .FirstOrDefault(b => b.Key.Symbol == item.Symbol
                                             && b.Key.TimeFactor == item.TimeFactor
                                             && b.Key.Timeframe == item.Timeframe).Value;
                    if (bars != null && bars.Count > 0)
                        data.Add(item, bars);
                }
            }
            else //need to request from data provider
            {
                foreach (var item in instruments)
                {
                    var btInstrument = (Selection)item.Clone();
                    if (BacktestSettings.BarsBack > 0)
                        btInstrument.BarCount = BacktestSettings.BarsBack;
                    if (BacktestSettings.StartDate.Year > 2000)
                        btInstrument.From = BacktestSettings.StartDate;
                    if (BacktestSettings.EndDate > BacktestSettings.StartDate)
                        btInstrument.To = BacktestSettings.EndDate;
                    var bars = DataProvider.GetBars(btInstrument);
                    if (bars != null && bars.Count > 0)
                        data.Add(btInstrument, bars);
                }
            }

            int batchSize = GetBacktestBatchSize(parameters);
            if (data.Count == 0 || batchSize < 1)
                return new List<TradeSignal>(0);

            // Scan all data collections
            var result = new List<TradeSignal>();
            var indices = data.ToDictionary(k => k.Key, v => 0);
            while (true)
            {
                // Get instrument with oldest/earliest data
                var time = DateTime.MaxValue;
                foreach (var item in data)
                {
                    var idx = indices[item.Key];
                    if (idx >= 0 && idx < item.Value.Count && item.Value[idx].Date < time)
                        time = item.Value[idx].Date;
                }

                if (time == DateTime.MaxValue)
                    break;

                var selectionsToUse = new List<Selection>();
                foreach (var item in data)
                {
                    var idx = indices[item.Key];
                    if (idx >= 0 && idx < item.Value.Count && item.Value[idx].Date == time)
                        selectionsToUse.Add(item.Key);
                }

                // Get necessary data frame to scan
                var dataFrames = new Dictionary<Selection, IEnumerable<Bar>>(selectionsToUse.Count);
                foreach (var item in data)
                {
                    if (selectionsToUse.Contains(item.Key))
                    {
                        dataFrames.Add(item.Key, item.Value.GetRange(indices[item.Key], batchSize));
                        indices[item.Key]++;
                    }
                }

                // Evaluate current batch
                if (dataFrames.Count > 0)
                {
                    var trades = Evaluate(dataFrames, parameters);

                    var barData = dataFrames.Keys.FirstOrDefault();
                    var barToProccess = dataFrames.Values.FirstOrDefault()?.FirstOrDefault();
                    SimulationBroker.ProcessBar(barData?.Symbol, barToProccess);

                    foreach (var account in BrokerAccounts)
                    {
                        foreach (var orderInfo in GenerateOrderParams(trades))
                        {
                            PlaceOrder(orderInfo, account);
                        }
                    }

                    if (trades != null && trades.Count > 0)
                        result.AddRange(trades);
                }

                // Break if backtest has been aborted
                if (State != SignalState.Backtesting && State != SignalState.BacktestingPaused)
                    break;
            }

            return result;
        }

        protected override OrderParams AnalyzePreTrade(OrderParams order)
        {
            // Your order details analyzer

            return order;
        }

        protected override void AnalyzePostTrade(Order order)
        {
            // Your order feedback analyzer
            foreach (var _account in BrokerAccounts)
            {
                if (_execTradesParam.HideSL == true || _execTradesParam.HideTP == true)
                {
                    ModifyOrder(order.UserID, _execTradesParam.SL, _execTradesParam.TP, _execTradesParam.HideSL,
                        _account);
                }
            }
        }

        protected override void ProcessTradeFailure(Order order, string error)
        {
            // Your order failure handler
        }

        protected override List<CodeParameterBase> InternalGetParameters()
        {
            return new List<CodeParameterBase>()
            {
                new StringParam("Start Event: ", "Chose On Which Event The Signal Calculation Is Triggered", 0)
                {
                    Value = "Periodical",
                    AllowedValues = new List<string>
                    {
                        "New Bar",
                        "New Tick",
                        "Periodical"
                    }
                },
                new IntParam("Start Event Frequency: ", "Only Start Event 'Periodical' Time in [ms]", 1)
                {
                    Value = 5000,
                    MinValue = 1,
                    MaxValue = 1000000
                },
                new IntParam("Trade Slot: ", "Chose Which Instruments Allowed For Trading", 2)
                {
                    Value = 1,
                    MinValue = 0,
                    MaxValue = 100
                },
                new StringParam("Order Type: ", "Chose Order Type", 3)
                {
                    Value = "Market",
                    AllowedValues = new List<string>
                    {
                        "Market",
                        "Limit",
                        "Stop Market"
                    }
                },
                new StringParam("TIF: ", "Time In Force", 4)
                {
                    Value = "FOK",
                    AllowedValues = new List<string>
                    {
                        "FOK",
                        "GFD",
                        "IOC",
                        "GTC"
                    }
                },
                new StringParam("Hide Order: ",
                    "Broker Don't See Limit and Stop Market Orders, SL and TP are hidden as well", 5)
                {
                    Value = "OFF",
                    AllowedValues = new List<string>
                    {
                        "ON",
                        "OFF"
                    }
                },
                new StringParam("Internal BackTest: ", "Signals Self Backtest During Normal Live Execution", 6)
                {
                    Value = "OFF",
                    AllowedValues = new List<string>
                    {
                        "ON",
                        "OFF"
                    }
                },
                new StringParam("BackTest Price Element: ", "Chose Which Price Data Element Is Processed", 7)
                {
                    Value = "CLOSE",
                    AllowedValues = new List<string>
                    {
                        "OPEN",
                        "HIGH",
                        "LOW",
                        "CLOSE",
                        "OHLC",
                        "OLHC"
                    }
                },
                new StringParam("Hide SL ", "Broker Don't See Stop Loss", 8)
                {
                    Value = "OFF",
                    AllowedValues = new List<string>
                    {
                        "ON",
                        "OFF"
                    }
                },
                new StringParam("Hide TP ", "Broker Don't See Take Profit", 9)
                {
                    Value = "OFF",
                    AllowedValues = new List<string>
                    {
                        "ON",
                        "OFF"
                    }
                },
                new IntParam("Quantity: ", "", 10)
                {
                    Value = 1,
                    MinValue = 0,
                    MaxValue = 100
                },
                new IntParam("Sell Price Offset: ", "For 1pip enter '10'!!!", 11)
                {
                    Value = 50,
                    MinValue = 0,
                    MaxValue = 10000
                },
                new IntParam("Buy Price Offset: ", "For 1pip enter '10'!!!", 12)
                {
                    Value = 50,
                    MinValue = 0,
                    MaxValue = 10000
                },
                new IntParam("Stop Loss Offset: ", "For 1pip enter '10'!!!", 13)
                {
                    Value = 0,
                    MinValue = 0,
                    MaxValue = 10000
                },
                new IntParam("Take Profit Offset: ", "For 1pip enter '10'!!!", 14)
                {
                    Value = 0,
                    MinValue = 0,
                    MaxValue = 10000
                },
                new IntParam("Backtest Batch: ", "Data Window For Backtesting", 15)
                {
                    Value = 1,
                    MinValue = 0,
                    MaxValue = 1000000000
                },
            };
        }

        protected override bool InternalSetParameters(List<CodeParameterBase> parameterBases)
        {
            var inputValue = ((StringParam)parameterBases[0]).Value;
            switch (inputValue)
            {
                case "New Bar":
                    _startMethod = StartMethod.NewBar;
                    break;
                case "New Tick":
                    _startMethod = StartMethod.NewQuote;
                    break;
                case "Periodical":
                    _startMethod = StartMethod.Periodic;
                    break;
                default:
                    Exit("Invalid Start Event Parameter.");
                    return false;
            }

            _tradingPeriod = ((IntParam)parameterBases[1]).Value;
            _tradeSlot = ((IntParam)parameterBases[2]).Value;

            inputValue = ((StringParam)parameterBases[3]).Value;
            switch (inputValue)
            {
                case "Market":
                    _execTradesParam.OrderType = TradeType.Market;
                    break;
                case "Limit":
                    _execTradesParam.OrderType = TradeType.Limit;
                    break;
                case "Stop Market":
                    _execTradesParam.OrderType = TradeType.Stop;
                    break;
                default:
                    Exit("Invalid Order Type Parameter.");
                    return false;
            }

            inputValue = ((StringParam)parameterBases[4]).Value;
            switch (inputValue)
            {
                case "FOK":
                    _execTradesParam.TIF = TimeInForce.FillOrKill;
                    break;
                case "GFD":
                    _execTradesParam.TIF = TimeInForce.GoodForDay;
                    break;
                case "IOC":
                    _execTradesParam.TIF = TimeInForce.ImmediateOrCancel;
                    break;
                case "GTC":
                    _execTradesParam.TIF = TimeInForce.GoodTilCancelled;
                    break;
                default:
                    Exit("Invalid TIF Parameter.");
                    return false;
            }

            inputValue = ((StringParam)parameterBases[5]).Value;
            switch (inputValue)
            {
                case "ON":
                    _execTradesParam.HideOrder = true;
                    break;
                case "OFF":
                    _execTradesParam.HideOrder = false;
                    break;
                default:
                    Exit("Invalid Hide Limit Order Parameter.");
                    return false;
            }

            inputValue = ((StringParam)parameterBases[6]).Value;
            switch (inputValue)
            {
                case "ON":
                    _internalBacktest = true;
                    break;
                case "OFF":
                    _internalBacktest = false;
                    break;
                default:
                    Exit("Invalid BackTest Mode Parameter.");
                    return false;
            }

            inputValue = ((StringParam)parameterBases[7]).Value;
            switch (inputValue)
            {
                case "OPEN":
                    backtestPriceConst = PriceConstants.OPEN;
                    break;
                case "HIGH":
                    backtestPriceConst = PriceConstants.HIGH;
                    break;
                case "LOW":
                    backtestPriceConst = PriceConstants.LOW;
                    break;
                case "CLOSE":
                    backtestPriceConst = PriceConstants.CLOSE;
                    break;
                case "OHLC":
                    backtestPriceConst = PriceConstants.OHLC;
                    break;
                case "OLHC":
                    backtestPriceConst = PriceConstants.OLHC;
                    break;
                default:
                    Exit("Invalid BackTest Price Parameter.");
                    return false;
            }

            inputValue = ((StringParam)parameterBases[8]).Value;
            switch (inputValue)
            {
                case "ON":
                    _execTradesParam.HideSL = true;
                    break;
                case "OFF":
                    _execTradesParam.HideSL = false;
                    break;
                default:
                    Exit("Invalid Hide Limit Order Parameter.");
                    return false;
            }

            inputValue = ((StringParam)parameterBases[9]).Value;
            switch (inputValue)
            {
                case "ON":
                    _execTradesParam.HideTP = true;
                    break;
                case "OFF":
                    _execTradesParam.HideTP = false;
                    break;
                default:
                    Exit("Invalid Hide Limit Order Parameter.");
                    return false;
            }

            _execTradesParam.OrderQuantity = ((IntParam)parameterBases[10]).Value;
            _execTradesParam.SellPriceOffset = ((IntParam)parameterBases[11]).Value;
            _execTradesParam.BuyPriceOffset = ((IntParam)parameterBases[12]).Value;
            _execTradesParam.SL = (decimal?)((IntParam)parameterBases[13]).Value > 0
                ? (decimal?)((IntParam)parameterBases[13]).Value / 100000
                : null;
            _execTradesParam.TP = (decimal?)((IntParam)parameterBases[14]).Value > 0
                ? (decimal?)((IntParam)parameterBases[14]).Value / 100000
                : null;
            _backtestBatch = ((IntParam)parameterBases[15]).Value;


            return true;
        }

        private int GetBacktestBatchSize(IEnumerable<object> values = null)
        {
            // Return the batch size for each backtest scan step of current instrument
            // For example, if history has 100 bars and you need at least 20 bars 
            // to calculate your indicators batch size would be equal to 20.

            // Example: return the largest integer value of provided parameters
            // (eg. if you have several period parameters for your signal's indicators
            // return values.Any() ? values.OfType<int>().Max() : 0;

            return _backtestBatch;
        }

        private List<TradeSignal> Evaluate(Dictionary<Selection, IEnumerable<Bar>> marketData,
            IEnumerable<object> parameterItem,
            Selection triggerInstrument = null,
            IEnumerable<Tick> ticks = null)
        {
            /* Evaluate supplied data bars using provided parameters 
               and return a collection of trades on successful evaluation
               Hint: you can pass these bars to your IndicatorBase instance in its Calculate() method
               and you can use current parameter values as that IndicatorBase parameters */

            var dataTickframes = marketData.Keys.Where(p => p.Timeframe == Timeframe.Tick);
            Dictionary<Selection, IEnumerable<Bar>> trigInstrData = new Dictionary<Selection, IEnumerable<Bar>>();

            #region Internal Backtest       

            if (_execTradesParam.EvalCount % 10 == 0 && _internalBacktest == true)
            {
                _internalBacktest = false;

                var backtestSet = new BacktestSettings
                {
                    InitialBalance = 10000,
                    TransactionCosts = 0,
                    Risk = 0,
                    BarsBack = 5,
                };

                Alert("----------------------------------");
                Alert("START Internal Backtest");
                Alert("----------------------------------");

                var res = Backtest(false);
                var tradeCount = res?[0].Summaries?.Select(i => i.NumberOfTradeSignals).DefaultIfEmpty(0)?.Sum() ?? 0;
                _internalBacktest = true;

                Alert("Evaluate(): Internal Backtest Trades: " + tradeCount);
                Alert("----------------------------------");
                Alert("STOP Internal Backtest");
                Alert("----------------------------------");
            }

            #endregion

            #region Prepare marketdata and pass it to trading logic for processing

            if (StartMethod == StartMethod.NewBar && triggerInstrument != null)
            {
                trigInstrData.Clear();
                trigInstrData.Add(triggerInstrument, DataProvider.GetBars(triggerInstrument));
            }

            if (State == SignalState.Backtesting) // && dataTickframes.Count() > 0)
            {
                var timer = new MicroStopwatch();
                timer.Start();

                var trades = new List<TradeSignal>();
                trades = BacktestPriceSegmentation.BacktestPriceSegmentProcessor(this, marketData, _execTradesParam,
                    backtestPriceConst, Calculate, trigInstrData, ticks);

                timer.Stop();
                Alert($"Init instrumentData: ExecutionTime = {timer.ElapsedMicroseconds:#,0} µs");

                return trades;
            }

            try
            {
                return Calculate(marketData);
            }

            catch (Exception e)
            {
                Alert($"Evaluate(): Failed to Run on Usercode: {e.Message}");
                return new List<TradeSignal>();
            }

            #endregion
        }

        #region BolingerBB

        //INPUTS in MQL version
        private const int InpMaPeriod = 9;            // averaging period iMA
        private const int InpBandsPeriod = 28;        // period for average iBands
        private const int InpDeviation = 2;           // number of standard deviations iBands //TODO bands dev in TI is Int = 2 in strategy double = 1.6
        private const double InpLots = 0.1;           // lots

        //VARIABLES
        private const int RequiredBarsCount = 30;

        //DATASTORAGE (needed for backtest)
        private Dictionary<Selection, IEnumerable<Bar>> _dataStorage = new Dictionary<Selection, IEnumerable<Bar>>();

        private List<TradeSignal> Calculate(Dictionary<Selection, IEnumerable<Bar>> marketData)
        {
            var signals = new List<TradeSignal>();

            if (State == SignalState.Backtesting)
            {
                foreach (var data in marketData)
                {
                    if (_dataStorage.TryGetValue(data.Key, out var neededValue))
                        _dataStorage[data.Key] = neededValue.Concat(data.Value);
                    else
                        _dataStorage.Add(data.Key, data.Value);
                }
            }
            else
                _dataStorage = marketData;

            //calculate for each selection
            foreach (var data in _dataStorage)
            {
                if (data.Value.Count() < RequiredBarsCount) continue;

                var smaInd = new SMA { Period = InpMaPeriod, Type = PriceConstants.CLOSE };
                smaInd.Init(null, DataProvider);
                smaInd.Calculate(data.Value);

                var bolingerInd = new Bands { Period = InpBandsPeriod, Deviation = InpDeviation, Type = PriceConstants.CLOSE };
                bolingerInd.Init(null, DataProvider);
                bolingerInd.Calculate(data.Value);

                if (smaInd.Series == null || smaInd.Series.Count == 0 || bolingerInd.Series == null || bolingerInd.Series.Count == 0) continue; //Check if calculated

                var ma1Long = SMAGet(smaInd.Series, 1);
                var ma2Long = SMAGet(smaInd.Series, 4);
                var bbMA = BandsGet(bolingerInd.Series, 0, 1);
                var bbUp = BandsGet(bolingerInd.Series, 1, 1);
                var bbLow = BandsGet(bolingerInd.Series, 2, 1);

                //--- closing Positions
                foreach (var account in BrokerAccounts)
                {
                    var positions = GetPositions(account, data.Key.Symbol);
                    var currentBar = data.Value.ElementAt(data.Value.Count() - 1);

                    if (!IsClosingPosition(data.Key, account) && positions != null && positions.Any())
                    {
                        foreach (var position in positions)
                        {
                            switch (position.PositionSide)
                            {
                                case Side.Buy when GetClose(data.Value, 1) < bbMA:
                                    signals.AddRange(GenerateClosePositionSignals(account, data.Key, currentBar.Date));
                                    Output(" > Closed position due to rules (Close < bbMA)");
                                    AddClosePositionMarker(data.Key, account);
                                    break;
                                case Side.Sell when GetClose(data.Value, 1) > bbMA:
                                    signals.AddRange(GenerateClosePositionSignals(account, data.Key, currentBar.Date));
                                    Output(" > Closed position due to rules (Close > bbMA)");
                                    AddClosePositionMarker(data.Key, account);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (GetClose(data.Value, 4) < bbUp && GetClose(data.Value, 1) > bbUp && ma1Long > ma2Long)
                        {
                            signals.Add(Open(data.Key, currentBar.CloseAsk, Side.Buy, currentBar.Date));
                            Output($" > Placed buy marked order on {currentBar.CloseAsk}");
                            RemoveClosePositionMarker(data.Key, account);
                        }
                        else if (GetClose(data.Value, 4) > bbLow && GetClose(data.Value, 1) < bbLow && ma1Long < ma2Long)
                        {
                            signals.Add(Open(data.Key, currentBar.CloseBid, Side.Sell, currentBar.Date));
                            Output($" > Placed sell marked order on {currentBar.CloseBid}");
                            RemoveClosePositionMarker(data.Key, account);
                        }
                    }
                }
            }

            return signals;
        }

        //+------------------------------------------------------------------+ 
        //| Get Close for specified bar index                                | 
        //+------------------------------------------------------------------+ 
        private static double GetClose(IEnumerable<Bar> bars, int index)
        {
            var barsList = bars.ToList();
            return (double)barsList[barsList.Count - ++index].MeanClose;
        }

        //+------------------------------------------------------------------+
        //| Get value of buffers for the iMA                                 |
        //+------------------------------------------------------------------+
        private static double SMAGet(IReadOnlyList<Series> sma, int index) => sma[0].Values[sma[0].Length - ++index].Value;

        //+------------------------------------------------------------------+
        //| Get value of buffers for the iBands                              |
        //|  the buffer numbers are the following:                           |
        //|   0 - BASE_LINE, 1 - UPPER_BAND, 2 - LOWER_BAND                  |
        //+------------------------------------------------------------------+
        private static double BandsGet(IReadOnlyList<Series> bb, int buffer, int index) => bb[buffer].Values[bb[buffer].Length - ++index].Value;

        //+------------------------------------------------------------------+
        //| Open position                                                |
        //+------------------------------------------------------------------+
        private TradeSignal Open(Selection selection, decimal price, Side side, DateTime barDate)
        {
            var time = State == SignalState.Backtesting ? barDate : DateTime.UtcNow;
            return new TradeSignal
            {
                Quantity = (decimal) InpLots,
                Instrument = selection,
                Price = price,
                Side = side,
                Time = time,
                TradeType = TradeType.Market,
                TimeInForce = TimeInForce.GoodTilCancelled
            };
        }

        #endregion

    }
}