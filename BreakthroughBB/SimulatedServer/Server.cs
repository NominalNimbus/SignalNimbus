using System;
using System.Collections.Generic;
using System.Linq;
using CommonObjects;
using DebugService.Classes;
using UserCode;
using UserCodeManager;
using AccountInfo = DebugService.Classes.AccountInfo;
using CodeParameterBase = DebugService.Classes.CodeParameterBase;
using Periodicity = CommonObjects.Timeframe;
using BacktestSettings = DebugService.Classes.BacktestSettings;

namespace SimulatedServer
{
    public class Server
    {
        private readonly System.Timers.Timer _tmrPeriodicExecution;
        private readonly System.Timers.Timer _tmrBacktestStatusCheck;
        private readonly List<HistoricalData> _selectedData;
        private DateTime _prevExecution;
        private float _backtestProgress;
        private IDataProvider _dataProvider; //simulated data provider
        private IBroker _broker;             //simulated broker

        public ICode Code; // User code instance (auto generated code)

        public List<CodeParameterBase>
            CodeParameters
        {
            get;
            private set;
        } //parameters of your code (will be added to this collection  automatically)

        public event EventHandler<string> OutputEvent; //append output string
        public event EventHandler BacktestFinished;

        public Server()
        {
            CreateTestManagment();
            _selectedData = new List<HistoricalData>();
            _tmrPeriodicExecution = new System.Timers.Timer(100);
            _tmrPeriodicExecution.Elapsed += RunOnTimer;
            _tmrBacktestStatusCheck = new System.Timers.Timer(200);
            _tmrBacktestStatusCheck.Elapsed += BacktestStatusCheck;
        }

        /// <summary>
        /// Start code execution or backtest
        /// </summary>
        /// <param name="historicalData">Available historical data</param>
        /// <param name="selectedData">Historical data to use</param>
        /// <param name="accounts">Available accounts</param>
        /// <param name="portfolios">Available portfolios</param>
        /// <param name="state">Signal state to use for signal (Running or Backtesting)</param>
        public void Start(List<HistoricalData> historicalData, List<HistoricalData> selectedData,
            List<AccountInfo> accounts, SignalState state)
        {
            if (!selectedData.Any())
            {
                RaiseOutput("Failed to start: selected data collection is empty");
                return;
            }

            _prevExecution = DateTime.MinValue;
            _selectedData.AddRange(selectedData);
            _dataProvider = new DataProvider(historicalData);
            ((DataProvider) _dataProvider).NewBarAppended += OnNewBarAppended;

            try
            {
                var msc = DateTime.UtcNow;
                var success = Code.SetParameters(CodeParameters.Select(ToExternalCodeParameters).ToList());
                if (!success)
                {
                    RaiseOutput("Failed to set user code parameters");
                    return;
                }

                if (Code is IndicatorBase)
                {
                    var indicator = Code as IndicatorBase;
                    success = indicator.Init(ToCodeSelection(_selectedData[0]), _dataProvider);
                    if (success)
                    {
                        var seconds = (DateTime.UtcNow - msc);
                        var maxRecords = 0;

                        if (indicator.Series.Count > 0)
                            indicator.Series.Select(series => series.Length).Max();

                        RaiseOutput(
                            $" *** Initialization: {indicator.Series.Count} series, {maxRecords} records, time: {seconds.TotalMilliseconds:0} ms{Environment.NewLine}");
                    }
                }
                else if (Code is SignalBase)
                {
                    var signal = Code as SignalBase;
                    _backtestProgress = -1;

                    _broker = new SimulationBroker(accounts.Select(a => new SimulationAccount
                    {
                        Balance = a.Balance,
                        Currency = a.Currency,
                        ID = a.ID,
                        Password = a.Password,
                        UserName = a.UserName
                    }));

                    success = signal.Init(_broker, _dataProvider, _selectedData.Select(ToCodeSelection).ToList(),
                        state, new StrategyParams(-1));
                    if (success)
                    {
                        var span = (DateTime.UtcNow - msc);
                        RaiseOutput($" *** Initialization time: {span.TotalMilliseconds:0} ms{Environment.NewLine}");
                        _tmrBacktestStatusCheck.Start();
                        if (state == SignalState.Running)
                        {
                            _tmrPeriodicExecution.Start();
                        }
                        else if (state == SignalState.Backtesting)
                        {
                            _backtestProgress = 0F;
                            RaiseOutput(DateTime.Now + "  > Starting backtest for " + signal.Name);
                            signal.StartBacktest(new CommonObjects.BacktestSettings());
                        }
                    }
                }

                if (!success)
                    RaiseOutput("Failed to initialize user code instance");
            }
            catch (Exception ex)
            {
                RaiseOutput(ex.ToString());
            }
        }

        /// <summary>
        ///  Stop User Code running
        /// </summary>
        public void Stop()
        {
            if (Code is SignalBase @base)
                @base.SetSignalState(SignalState.Stopped);
            _tmrBacktestStatusCheck.Stop();
            _tmrPeriodicExecution.Stop();
            ((DataProvider) _dataProvider).NewBarAppended -= OnNewBarAppended;
            Code = null;
            _broker = null;
            _selectedData.Clear();
            _dataProvider = null;
            CreateTestManagment();
            RaiseOutput(" *** Stopped");
        }

        /// <summary>
        /// Append new tick to Data Provider and run User Code
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="data"></param>
        public void AppendTick(Quote tick, HistoricalData data)
        {
            if (_dataProvider != null)
                ((DataProvider) _dataProvider).AppendTick(tick, data);

            if (_selectedData != null &&
                _selectedData.Any(i => i.SecurityID == data.SecurityID && i.DataFeed == data.DataFeed))
                RunOnNewQuote(data);
        }

        /// <summary>
        /// Create new instance of user code and refreshing parameters, required in case of portfolio or account list change
        /// </summary>
        public void Refresh()
        {
            CreateTestManagment();
        }

        #region Data converters

        private static CodeParameterBase ToCodeParameters(CommonObjects.CodeParameterBase parameter)
        {
            if (parameter is CommonObjects.IntParam)
            {
                return new DebugService.Classes.IntParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((CommonObjects.IntParam) parameter).Value,
                    MinValue = ((CommonObjects.IntParam) parameter).MinValue,
                    MaxValue = ((CommonObjects.IntParam) parameter).MaxValue
                };
            }

            if (parameter is CommonObjects.DoubleParam)
            {
                return new DebugService.Classes.DoubleParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((CommonObjects.DoubleParam) parameter).Value,
                    MinValue = ((CommonObjects.DoubleParam) parameter).MinValue,
                    MaxValue = ((CommonObjects.DoubleParam) parameter).MaxValue
                };
            }

            if (parameter is CommonObjects.ColorParam)
            {
                return new DebugService.Classes.ColorParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    ColorString = ((CommonObjects.ColorParam) parameter).Value.ToString()
                };
            }

            if (parameter is CommonObjects.StringParam)
            {
                return new DebugService.Classes.StringParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((CommonObjects.StringParam) parameter).Value,
                    AllowedValues = ((CommonObjects.StringParam) parameter).AllowedValues.ToList()
                };
            }

            if (parameter is CommonObjects.SeriesParam)
            {
                return new DebugService.Classes.SeriesParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Thickness = ((CommonObjects.SeriesParam) parameter).Thickness,
                    ColorString = ((CommonObjects.SeriesParam) parameter).Color.ToString(),
                };
            }

            return null;
        }

        private static Selection ToCodeSelection(HistoricalData data)
        {
            return new Selection
            {
                BarCount = data.Bars.Count,
                DataFeed = data.DataFeed,
                Symbol = data.Symbol,
                TimeFactor = data.Interval,
                Timeframe = ToExternalPeriodicity(data.Periodicity),
                MarketDataSlot = data.Slot
            };
        }

        private static CommonObjects.CodeParameterBase ToExternalCodeParameters(CodeParameterBase parameter)
        {
            if (parameter is DebugService.Classes.IntParam)
            {
                var p = ((DebugService.Classes.IntParam) parameter);
                //TODO (optional): use parameter space values (start, step, stop) for backtesting
                return new CommonObjects.IntParam(parameter.Name, parameter.Category, parameter.ID)
                {
                    Value = p.Value,
                    MinValue = p.MinValue,
                    MaxValue = p.MaxValue,
                    StartValue = p.Value,
                    Step = 1,
                    StopValue = p.Value
                };
            }

            if (parameter is DebugService.Classes.DoubleParam)
            {
                var p = ((DebugService.Classes.DoubleParam) parameter);
                //TODO (optional): use parameter space values (start, step, stop) for backtesting
                return new CommonObjects.DoubleParam(parameter.Name, parameter.Category, parameter.ID)
                {
                    Value = p.Value,
                    MinValue = p.MinValue,
                    MaxValue = p.MaxValue,
                    StartValue = p.Value,
                    Step = 1,
                    StopValue = p.Value
                };
            }

            if (parameter is DebugService.Classes.ColorParam)
            {
                return new CommonObjects.ColorParam(parameter.Name, parameter.Category, parameter.ID)
                {
                    Value = ((DebugService.Classes.ColorParam) parameter).Value
                };
            }

            if (parameter is DebugService.Classes.StringParam)
            {
                return new CommonObjects.StringParam(parameter.Name, parameter.Category, parameter.ID)
                {
                    Value = ((DebugService.Classes.StringParam) parameter).Value,
                    AllowedValues = ((DebugService.Classes.StringParam) parameter).AllowedValues.ToList()
                };
            }

            if (parameter is DebugService.Classes.SeriesParam)
            {
                return new CommonObjects.SeriesParam(parameter.Name, parameter.Category, parameter.ID)
                {
                    Thickness = ((DebugService.Classes.SeriesParam) parameter).Thickness,
                    Color = ((DebugService.Classes.SeriesParam) parameter).Color,
                };
            }

            return null;
        }

        private static Periodicity ToExternalPeriodicity(DebugService.Classes.Periodicity p)
        {
            switch (p)
            {
                case DebugService.Classes.Periodicity.Minute: return Periodicity.Minute;
                case DebugService.Classes.Periodicity.Hour: return Periodicity.Hour;
                case DebugService.Classes.Periodicity.Day: return Periodicity.Day;
                case DebugService.Classes.Periodicity.Month: return Periodicity.Month;
                default: throw new ArgumentException();
            }
        }

        private static CommonObjects.BacktestSettings ToExternalBacktestSettings(BacktestSettings settings)
        {
            if (settings == null)
                return null;

            return new CommonObjects.BacktestSettings
            {
                BarsBack = settings.BarsBack,
                StartDate = settings.StartDate,
                EndDate = settings.EndDate,
                InitialBalance = settings.InitialBalance,
                Risk = settings.Risk,
                TransactionCosts = settings.TransactionCosts
            };
        }

        #endregion

        #region Private methods

        private void CreateTestManagment()
        {
            // Auto generated code
            Code = new global::TestManagment.BreakthroughBB();

            CodeParameters = new List<CodeParameterBase>(Code.GetParameters().Select(ToCodeParameters));
        }

        private void RunOnNewQuote(HistoricalData data)
        {
            try
            {
                var msc = DateTime.UtcNow;
                if (Code is IndicatorBase)
                {
                    var indicator = Code as IndicatorBase;
                    var res = indicator.Calculate();
                    var seconds = (DateTime.UtcNow - msc);
                    var seriesLength = indicator.Series.Select(series => series.Length).ToList();
                    var maxRecords = seriesLength.Count > 0 ? seriesLength.Max() : 0;
                    RaiseOutput(
                        $" > On new quote run: series count - {indicator.Series.Count}, records count - {maxRecords}, {res} items recalculated, time: {seconds.ToString("g")}");
                }
                else if (Code is SignalBase)
                {
                    var signal = Code as SignalBase;
                    if (signal.StartMethod != StartMethod.NewQuote || !signal.IsReadyToRun)
                        return;

                    if (signal.StartMethod == StartMethod.Once && _prevExecution != DateTime.MinValue)
                        return;

                    _prevExecution = DateTime.UtcNow;
                    signal.Start(ToCodeSelection(data));
                    var seconds = (DateTime.UtcNow - msc);
                    RaiseOutput(" > On new quote start, time: " + seconds.ToString("g"));

                    var alerts = Code.GetActualAlerts();
                    alerts.AddRange(((SimulationBroker) _broker).ActivityLog);
                    foreach (var alert in alerts)
                        RaiseOutput(" - Alert: " + alert);
                }
            }
            catch (Exception ex)
            {
                RaiseOutput(ex.ToString());
                throw;
            }
        }

        private void OnNewBarAppended(object sender, HistoricalData data)
        {
            try
            {
                if (Code is SignalBase)
                {
                    var signal = (SignalBase) Code;
                    if (signal.StartMethod != StartMethod.NewBar || !signal.IsReadyToRun)
                        return;

                    _prevExecution = DateTime.UtcNow;
                    signal.Start(ToCodeSelection(data));
                    var seconds = (DateTime.UtcNow - _prevExecution);
                    RaiseOutput($" > On new bar start, time: {seconds.ToString("g")}");

                    var alerts = signal.GetActualAlerts();
                    alerts.AddRange(((SimulationBroker) _broker).ActivityLog);
                    if (alerts.Count > 0)
                    {
                        foreach (var alert in alerts)
                            RaiseOutput($" - Alert: {alert}");
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseOutput(ex.ToString());
                throw;
            }
        }

        private void RunOnTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            var signal = Code as SignalBase;

            if (signal?.StartMethod != StartMethod.Periodic || !signal.IsReadyToRun)
                return;

            var elapsed = (DateTime.UtcNow - _prevExecution).TotalMilliseconds;
            if (signal.ExecutionPeriod < 1 || elapsed <= signal.ExecutionPeriod)
                return;

            foreach (var output in signal.GetOutputs())
                RaiseOutput($"{output.DateTime} {output.Message}");

            try
            {
                _prevExecution = DateTime.UtcNow;
                signal.Start();
                var seconds = (DateTime.UtcNow - _prevExecution);
                RaiseOutput($"{DateTime.Now}  > Periodic signal execution, time: {seconds.ToString("g")}");

                var alerts = signal.GetActualAlerts();
                alerts.AddRange(((SimulationBroker) _broker).ActivityLog);
                if (alerts.Count > 0)
                {
                    foreach (var alert in alerts)
                        RaiseOutput($" - Alert: {alert}");
                }
            }
            catch (Exception ex)
            {
                RaiseOutput(ex.ToString());
                throw;
            }
        }

        private void BacktestStatusCheck(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_backtestProgress < 0F) //backtest is not currently running
                return;
            if (!(Code is SignalBase signal)) return;

            _tmrBacktestStatusCheck.Enabled = false;
            if (signal.BacktestProgress > _backtestProgress + 5F)
                RaiseOutput($"{DateTime.Now}  > Backtest progress: {signal.BacktestProgress:0}%");

            _backtestProgress = signal.BacktestProgress;
            if (_backtestProgress >= 100F)
            {
                var results = signal.BacktestResults;
                var summary = new System.Text.StringBuilder();
                summary.AppendLine($"     Generated {GetBacktestTradeCount(results)} trades");
                //TODO: add other stats if necessary

                RaiseOutput(DateTime.Now + "  > Finished backtest: " + Environment.NewLine + summary.ToString());
                _backtestProgress = -1F;
                BacktestFinished?.Invoke(this, EventArgs.Empty);
            }

            foreach (var output in signal.GetOutputs())
                RaiseOutput($"{output.DateTime} {output.Message}");

            _tmrBacktestStatusCheck.Enabled = true;
        }

        private void RaiseOutput(string message)
        {
            OutputEvent?.Invoke(this, message);
        }

        private static int GetBacktestTradeCount(List<BacktestResults> results)
        {
            if (results == null || results.Count == 0)
                return 0;

            int sum = 0;
            foreach (var r in results)
            {
                if (r.Summaries != null && r.Summaries.Count > 0)
                    sum += r.Summaries.Sum(i => i.NumberOfTradeSignals);
            }

            return sum;
        }

        #endregion
    }
}