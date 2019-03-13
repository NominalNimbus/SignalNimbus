using System;
using System.Collections.Generic;
using System.Linq;
using CommonObjects;
using UserCode;
using Auxiliaries;
using UserCode.TI;

namespace FibonacciRetrecements
{
    /// FibonacciRetrecements signal
    public class FibonacciRetrecements : SignalBase
    {
        private bool _internalBacktest;
        public StartMethod _startMethod;
        private ExecuteTradesParam _execTradesParam;
        private PriceConstants backtestPriceConst;
        private int _backtestBatch;
        public int _tradeSlot { get; private set; }
        public int _tradingPeriod { get; private set; }
        public Dictionary<Selection, IEnumerable<Bar>> _instrumentData { get; private set; }


        public FibonacciRetrecements()
        {
            Name = "FibonacciRetrecements";
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
            if (trades != null && trades.Count > 0)
            {
                // Perform some action based on the trade details
                //ExecuteTrades.Execute(trades, this, _execTradesParam);
                foreach (var account in BrokerAccounts)
                {
                    foreach (var tradeSignal in trades)
                    {
                        var orderInfo = new OrderParams
                        {
                            Symbol = tradeSignal.Instrument.Symbol,
                            TimeInForce = tradeSignal.TimeInForce,
                            Quantity = tradeSignal.Quantity,
                            Price = tradeSignal.Price,
                            OrderType = tradeSignal.TradeType,
                            OrderSide = tradeSignal.Side,
                            UserID = Guid.NewGuid().ToString()
                        };

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
                    var btInstrument = (Selection) item.Clone();
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
                        foreach (var tradeSignal in trades)
                        {
                            var orderInfo = new OrderParams
                            {
                                Symbol = tradeSignal.Instrument.Symbol,
                                TimeInForce = tradeSignal.TimeInForce,
                                Quantity = tradeSignal.Quantity,
                                Price = tradeSignal.Price,
                                OrderType = tradeSignal.TradeType,
                                OrderSide = tradeSignal.Side,
                                UserID = Guid.NewGuid().ToString()
                            };

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
            var inputValue = ((StringParam) parameterBases[0]).Value;
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

            _tradingPeriod = ((IntParam) parameterBases[1]).Value;
            _tradeSlot = ((IntParam) parameterBases[2]).Value;

            inputValue = ((StringParam) parameterBases[3]).Value;
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

            inputValue = ((StringParam) parameterBases[4]).Value;
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

            inputValue = ((StringParam) parameterBases[5]).Value;
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

            inputValue = ((StringParam) parameterBases[6]).Value;
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

            inputValue = ((StringParam) parameterBases[7]).Value;
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

            inputValue = ((StringParam) parameterBases[8]).Value;
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

            inputValue = ((StringParam) parameterBases[9]).Value;
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

            _execTradesParam.OrderQuantity = ((IntParam) parameterBases[10]).Value;
            _execTradesParam.SellPriceOffset = ((IntParam) parameterBases[11]).Value;
            _execTradesParam.BuyPriceOffset = ((IntParam) parameterBases[12]).Value;
            _execTradesParam.SL = (decimal?) ((IntParam) parameterBases[13]).Value > 0
                ? (decimal?) ((IntParam) parameterBases[13]).Value / 100000
                : null;
            _execTradesParam.TP = (decimal?) ((IntParam) parameterBases[14]).Value > 0
                ? (decimal?) ((IntParam) parameterBases[14]).Value / 100000
                : null;
            _backtestBatch = ((IntParam) parameterBases[15]).Value;


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

        #region FibonacciRetracement

        //INPUTS
        int _tradeValue = 1;          //Trade volume
        int _safetyBuffer = 1;        //Next bar close distance from level (points)
        int _trendPrecision = -5;     //Next to previous high(low) distance
        int _stopLossLevel = 15;      //Stop-loss level (points)
        decimal _takeProfitAt = 0.2m; //Take profit at FIBO extension

        //VARIABLES
        private const int RequiredBarsCount = 100;
        private const decimal Point = 0.01m;

        List<decimal> _zzBuffer = new List<decimal>();
        readonly decimal[] _hl = new decimal[4];
        readonly DateTime[] _hlTime = new DateTime[4];

        private int _trendDirection;
        private decimal _fibo00;
        private decimal _fibo23;
        private decimal _fibo38;
        private decimal _fibo61;
        private decimal _fibo76;
        private decimal _fibo100;
        private decimal _fiboBase;

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

                //MQL Compatibility Methods
                decimal GetCurrentBid() => data.Value.Last()?.CloseBid ?? 0m;
                decimal GetCurrentAsk() => data.Value.Last()?.CloseAsk ?? 0m;
                DateTime GetCurrentDate() => data.Value.Last()?.Date ?? DateTime.Now;

                Bar RatesArray(int index) => data.Value.ElementAt(data.Value.Count() - ++index);
                ///////////////////////////

                var zigZag = new ZigZag();
                zigZag.Init(null, DataProvider);
                zigZag.Calculate(data.Value);
                _zzBuffer = new List<decimal>(zigZag.Series[0].Values.Select(s => (decimal) s.Value));

                _hl[0] = 666;

                if (_hl[0] == 666)
                {
                    var zCount = 0;
                    for (var i = 0; i <= _zzBuffer.Count && zCount <= 3; i++)
                    {
                        if (_zzBuffer[i] == 0) continue;

                        _hl[zCount] = _zzBuffer[i];
                        _hlTime[zCount] = RatesArray(i).Date;
                        zCount++;
                    }

                    _trendDirection = CheckTrend(_hl[0], _hl[1], _hl[2], _hl[3]);
                    _fibo00 = _hl[0];
                    _fibo100 = _hl[1];
                    _fiboBase = Math.Abs(_fibo100 - _fibo00);
                }

                // high-low points mapping in concequence
                if (_zzBuffer[0] != 0 && _zzBuffer[0] != _hl[0])
                {
                    _hl[3] = _hl[2];
                    _hl[2] = _hl[1];
                    _hl[1] = _hl[0];
                    _hl[0] = _zzBuffer[0];
                    _hlTime[3] = _hlTime[2];
                    _hlTime[2] = _hlTime[1];
                    _hlTime[1] = _hlTime[0];
                    _hlTime[0] = RatesArray(0).Date;
                    _trendDirection = CheckTrend(_hl[0], _hl[1], _hl[2], _hl[3]);
                }

                if (BrokerAccounts == null) continue;
                foreach (var brokerAccount in BrokerAccounts)
                {
                    if (GetPositions(brokerAccount, data.Key.Symbol)?.Count != 0) continue;

                    switch (_trendDirection)
                    {
                        case 1:
                            _fiboBase = _fibo00 - _fibo100;
                            _fibo23 = _fibo00 - 0.236m * _fiboBase;
                            _fibo38 = _fibo00 - 0.382m * _fiboBase;
                            _fibo61 = _fibo00 - 0.618m * _fiboBase;
                            _fibo76 = _fibo00 - 0.764m * _fiboBase;
                            if (_hl[0] > _fibo00)
                            {
                                _fibo00 = _hl[0];
                                if (_hl[0] - _hl[1] > _fiboBase)
                                {
                                    _fibo100 = _hl[1];
                                }
                            }

                            if (_hl[0] < _fibo100)
                            {
                                _fibo00 = _hl[0];
                                _fibo100 = _hl[1];
                                _trendDirection = CheckTrend(_hl[0], _hl[1], _hl[2], _hl[3]);
                            }

                            if (RatesArray(0).MeanClose - _fibo76 > Point * _safetyBuffer &&
                                _fibo76 - RatesArray(1).MeanClose > Point * _safetyBuffer ||
                                RatesArray(0).MeanClose - _fibo61 > Point * _safetyBuffer &&
                                _fibo61 - RatesArray(1).MeanClose > Point * _safetyBuffer ||
                                RatesArray(0).MeanClose - _fibo38 > Point * _safetyBuffer &&
                                _fibo38 - RatesArray(1).MeanClose > Point * _safetyBuffer ||
                                RatesArray(0).MeanClose - _fibo23 > Point * _safetyBuffer &&
                                _fibo23 - RatesArray(1).MeanClose > Point * _safetyBuffer)
                            {
                                var signal = TradeCheck(1, data.Key, GetCurrentBid(), GetCurrentAsk(), GetCurrentDate());
                                if (signal != null) signals.Add(signal);
                            }

                            break;
                        case -1:
                            _fiboBase = _fibo100 - _fibo00;
                            _fibo23 = _fibo00 + 0.236m * _fiboBase;
                            _fibo38 = _fibo00 + 0.382m * _fiboBase;
                            _fibo61 = _fibo00 + 0.618m * _fiboBase;
                            _fibo76 = _fibo00 + 0.764m * _fiboBase;
                            if (_hl[0] < _fibo00)
                            {
                                _fibo00 = _hl[0];
                                if (_hl[1] - _hl[0] > _fiboBase)
                                {
                                    _fibo100 = _hl[1];
                                }
                            }

                            if (_hl[0] > _fibo100)
                            {
                                _fibo00 = _hl[0];
                                _fibo100 = _hl[1];
                                _trendDirection = CheckTrend(_hl[0], _hl[1], _hl[2], _hl[3]);
                            }

                            if (_fibo76 - RatesArray(0).MeanClose > Point * _safetyBuffer &&
                                _fibo76 - RatesArray(1).MeanClose < Point * _safetyBuffer ||
                                _fibo61 - RatesArray(0).MeanClose > Point * _safetyBuffer &&
                                _fibo61 - RatesArray(1).MeanClose < Point * _safetyBuffer ||
                                _fibo38 - RatesArray(0).MeanClose > Point * _safetyBuffer &&
                                _fibo38 - RatesArray(1).MeanClose < Point * _safetyBuffer ||
                                _fibo23 - RatesArray(0).MeanClose > Point * _safetyBuffer &&
                                _fibo23 - RatesArray(1).MeanClose < Point * _safetyBuffer)
                            {
                                var signal = TradeCheck(-1, data.Key, GetCurrentBid(), GetCurrentAsk(), GetCurrentDate());
                                if (signal != null) signals.Add(signal);
                            }

                            break;
                        case 0:
                            _fiboBase = 0;
                            _fibo23 = 0;
                            _fibo38 = 0;
                            _fibo61 = 0;
                            _fibo76 = 0;
                            break;
                    }
                }
            }

            return signals;
        }

        //+------------------------------------------------------------------+
        //| TradeCheck function                                              |
        //+------------------------------------------------------------------+
        private TradeSignal TradeCheck(int deal, Selection selection, decimal bid, decimal ask, DateTime time)
        {
            var dateTime = State == SignalState.Backtesting ? time : DateTime.UtcNow;
            switch (deal)
            {
                case 1:
                    return new TradeSignal
                    {
                        Instrument = selection,
                        Price = bid,
                        Quantity = _tradeValue,
                        Side = Side.Buy,
                        Time = dateTime,
                        TimeInForce = TimeInForce.GoodTilCancelled,
                        TradeType = TradeType.Market,
                        SLOffset = bid - Point * _stopLossLevel,
                        TPOffset = ToOffset(bid, _fibo00 + _takeProfitAt * _fiboBase)
                    };
                case 2:
                    return new TradeSignal
                    {
                        Instrument = selection,
                        Price = ask,
                        Quantity = _tradeValue,
                        Side = Side.Sell,
                        Time = dateTime,
                        TimeInForce = TimeInForce.GoodTilCancelled,
                        TradeType = TradeType.Market,
                        SLOffset = ask + Point * _stopLossLevel,
                        TPOffset = ToOffset(ask, _fibo00 - _takeProfitAt * _fiboBase)
                    };
                default:
                    return null;
            }
        }

        //+------------------------------------------------------------------+
        //| CheckTrend function                                              |
        //+------------------------------------------------------------------+
        private int CheckTrend(decimal hl0, decimal hl1, decimal hl2, decimal hl3)
        {
            if (hl2 - hl0 > Point * _trendPrecision && hl3 - hl1 > Point * _trendPrecision) return -1; // trend is down
            if (hl0 - hl2 > Point * _trendPrecision && hl1 - hl3 > Point * _trendPrecision) return 1;  // trend is up
            return 0;
        }

        private static decimal ToOffset(decimal price, decimal limitPrice) => Math.Abs(price - limitPrice);

        #endregion

    }
}