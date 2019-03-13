using System;
using System.Linq;
using System.Windows;
using DebugService;
using DebugService.Classes;

namespace SimulatedServer
{
    public partial class App : Application
    {
        private Server _server;
        private MainWindow _window;

        public App()
        {
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _server = new Server();
            _window = new MainWindow(_server.Code.Name, _server.CodeParameters);

            _window.CodeStateChanged += WindowOnStateChanged;
            _window.ManuallyTickAdded += WindowOnManuallyTickAdded;
            _window.RefreshCodeParametersNeeded += WindowOnRefreshCodeParametersNeeded;

            _server.OutputEvent += (s, msg) => _window.Output += (msg + Environment.NewLine);
            _server.BacktestFinished += (s, x) => WindowOnStateChanged(_window, State.Stopped);

            _window.ShowDialog();
        }

        private void WindowOnRefreshCodeParametersNeeded(object sender, EventArgs args)
        {
            _server.Refresh();
            _window.SetCodeParameters(_server.CodeParameters);
        }

        private void WindowOnManuallyTickAdded(object sender, Quote quote)
        {
            _server.AppendTick(quote, _window.SelectedHistoricalData[0] as HistoricalData);
        }

        private void WindowOnStateChanged(object sender, State state)
        {
            _window.State = state;
            switch (state)
            {
                case State.Running:
                case State.Backtesting:
                    _window.Output = String.Empty;
                    _server.Start(_window.HistoricalData.ToList(),
                        _window.SelectedHistoricalData.Cast<HistoricalData>().ToList(),
                        _window.Accounts.ToList(),
                        (CommonObjects.SignalState)state);

                    foreach (var data in _window.SelectedHistoricalData.Cast<HistoricalData>())
                    foreach (var quote in data.Quotes)
                        _server.AppendTick(quote, data);
                    break;

                case State.Stopped:
                    _server.Stop();
                    break;
            }
        }
    }
}
