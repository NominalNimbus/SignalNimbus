using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;
using DebugService.Annotations;
using DebugService.Classes;
using Microsoft.Win32;

namespace DebugService
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private string _name;
        private State _state;
        private string _output;
        private System.Collections.IList _selectedHistoricalData;
        private double _bid;
        private double _ask;
        private DateTime _date;
        private double _askSize;
        private double _bidSize;
        private Random _random;
        private AccountInfo _selectedAccount;

        #region Properties and Events
        public double Bid
        {
            get { return _bid; }
            set
            {
                if (value.Equals(_bid)) return;
                _bid = value;
                OnPropertyChanged("Bid");
            }
        }

        public double Ask
        {
            get { return _ask; }
            set
            {
                if (value.Equals(_ask)) return;
                _ask = value;
                OnPropertyChanged("Ask");
            }
        }

        public double AskSize
        {
            get { return _askSize; }
            set
            {
                if (value.Equals(_askSize)) return;
                _askSize = value;
                OnPropertyChanged("AskSize");
            }
        }

        public double BidSize
        {
            get { return _bidSize; }
            set
            {
                if (value.Equals(_bidSize)) return;
                _bidSize = value;
                OnPropertyChanged("BidSize");
            }
        }

        public DateTime Date
        {
            get { return _date; }
            set
            {
                if (value.Equals(_date)) return;
                _date = value;
                OnPropertyChanged("Date");
            }
        }

        public State State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged("State");
                    OnPropertyChanged("WindowTitle");
                    OnPropertyChanged("StartStopAction");
                }
            }
        }

        public string CodeName
        {
            get { return _name ?? "User Code"; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("CodeName");
                    OnPropertyChanged("WindowTitle");
                }
            }
        }

        public string WindowTitle => String.Format("{0}: {1}", CodeName, State);

        public string StartStopAction => State == State.Stopped ? "S_tart" : "S_top";

        public string Output
        {
            get { return _output; }
            set
            {
                if (value == _output) return;
                _output = value;
                OnPropertyChanged("Output");
            }
        }

        /// <summary>
        /// Currently selected historical data
        /// </summary>
        public System.Collections.IList SelectedHistoricalData
        {
            get { return _selectedHistoricalData; }
            set
            {
                if (value == null)
                {
                    if (_selectedHistoricalData != null)
                    {
                        _selectedHistoricalData = null;
                        OnPropertyChanged("SelectedHistoricalData");
                    }
                    return;
                }

                _selectedHistoricalData = value;
                OnPropertyChanged("SelectedHistoricalData");

                if (_selectedHistoricalData != null && _selectedHistoricalData.Count > 0)
                {
                    foreach (var item in _selectedHistoricalData.Cast<HistoricalData>())
                    {
                        if (item.Quotes.Count > 0)
                        {
                            var price = (double)item.Quotes.Last().BidPrice * GetRandomCoefficient();
                            var price1 = (double)item.Quotes.Last().AskPrice * GetRandomCoefficient();
                            Bid = Math.Max(price, price1);
                            Ask = Math.Min(price, price1);
                            BidSize = (double)item.Quotes.Last().BidSize * GetRandomCoefficient();
                            AskSize = (double)item.Quotes.Last().AskSize * GetRandomCoefficient();
                            Date = item.Quotes.Last().Time.AddSeconds(30);
                        }
                        else if (item.Bars.Count > 0)
                        {
                            var price = (double)item.Bars.Last().MeanClose * GetRandomCoefficient();
                            var price1 = (double)item.Bars.Last().MeanClose * GetRandomCoefficient();
                            Bid = Math.Max(price, price1);
                            Ask = Math.Min(price, price1);
                            BidSize = item.Bars.Last().MeanVolume * GetRandomCoefficient() / 10;
                            AskSize = item.Bars.Last().MeanVolume * GetRandomCoefficient() / 10;
                            Date = item.Bars.Last().Timestamp.AddSeconds(30);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Currently selected broker account info
        /// </summary>
        public AccountInfo SelectedAccount
        {
            get { return _selectedAccount; }
            set
            {
                if (Equals(value, _selectedAccount)) return;
                _selectedAccount = value;
                OnPropertyChanged("SelectedAccount");
            }
        }

        /// <summary>
        /// List of all available historical data (loaded from files and created as simulated)
        /// </summary>
        public ObservableCollection<HistoricalData> HistoricalData { get; private set; }

        /// <summary>
        /// User code parameters
        /// </summary>
        public ObservableCollection<CodeParameterBase> Parameters { get; private set; }

        /// <summary>
        /// List of simulated broker account which will be used for signals
        /// </summary>
        public ObservableCollection<AccountInfo> Accounts { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise when user initiates/stops signal execution/backtest
        /// </summary>
        public event EventHandler<State> CodeStateChanged;

        /// <summary>
        /// Raise when user click on Append Tick button
        /// </summary>
        public event EventHandler<Quote> ManuallyTickAdded;

        /// <summary>
        /// Ask refreshed code parameters
        /// </summary>
        public event EventHandler RefreshCodeParametersNeeded;
        #endregion

        public MainWindow(string codeName, IEnumerable<CodeParameterBase> parameters)
        {
            DataContext = this;
            CodeName = codeName;
            Output = String.Empty;
            HistoricalData = new ObservableCollection<HistoricalData>();
            Parameters = new ObservableCollection<CodeParameterBase>(parameters);
            Accounts = new ObservableCollection<AccountInfo>();
            _random = new Random(0);
            InitializeComponent();
        }

        #region Event Handlers
        private void mnuStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (State == State.Stopped)
            {

                if (SelectedHistoricalData == null)
                {
                    MessageBox.Show("Select historical data on which your code will be started.", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                State = State.Running;
                Output = String.Empty;
                GetRandomCoefficient();
                CodeStateChanged?.Invoke(this, State);
            }
            else
            {
                if (State != State.Stopped)
                {
                    State = State.Stopped;
                    CodeStateChanged?.Invoke(this, State);
                }
            }
        }

        private void mnuBacktest_Click(object sender, RoutedEventArgs e)
        {
            if (State == State.Backtesting)
                return;

            if (SelectedHistoricalData == null)
            {
                MessageBox.Show("Select historical data on which your code will be started.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            State = State.Backtesting;
            CodeStateChanged?.Invoke(this, State);
        }

        private void mnuLoadWorkspace_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Title = "Load Workspace";
            dlg.Filter = "XML Files|*.xml";
            var dialogResult = dlg.ShowDialog();
            if (!dialogResult.HasValue || !dialogResult.Value)
                return;

            var file = dlg.FileName;
            if (String.IsNullOrEmpty(file) || !File.Exists(file))
                return;

            try
            {
                Workspace ws = null;
                using (TextReader textReader = new StreamReader(file, System.Text.Encoding.UTF8))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Workspace));
                    ws = (Workspace)xmlSerializer.Deserialize(textReader);
                }

                ApplyWorkspace(ws);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Failed to load workspace", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuSaveWorkspace_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "XML Files|*.xml";
            dlg.Title = "Save Workspace";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog().Value != true)
                return;

            Workspace ws = GetWorkspace();
            if (ws == null)
                return;

            try
            {
                var ns = new XmlSerializerNamespaces();  //optional: strip namespace stuff
                ns.Add("", "");
                using (System.Xml.XmlTextWriter textWriter = new System.Xml
                    .XmlTextWriter(dlg.FileName, System.Text.Encoding.UTF8))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Workspace));
                    textWriter.Formatting = System.Xml.Formatting.Indented;
                    xmlSerializer.Serialize(textWriter, ws, ns);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Failed to load workspace",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }

        private void btnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files|*.csv|Text Files|*.txt";
            var dialogResult = openFileDialog.ShowDialog();

            if (!dialogResult.HasValue || !dialogResult.Value) return;
            var fileName = openFileDialog.FileName;
            if (String.IsNullOrEmpty(fileName))
                return;

            var result = GlobalHelper.LoadDataFromCSV(fileName);
            
            if (result == null)
            {
                MessageBox.Show("Failed to load data from file " + fileName,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (HistoricalData.Any(p => p.Symbol.Equals(result.Symbol) && p.DataFeed.Equals(result.DataFeed) &&
                                            p.Periodicity == result.Periodicity && p.Interval == result.Interval))
                {
                    MessageBox.Show("Historical data with same parameters already loaded",
                   "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                HistoricalData.Add(result);
            }
        }

        private void btnRemoveHistoricalItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedHistoricalData != null && SelectedHistoricalData.Count > 0)
            {
                for (int i = SelectedHistoricalData.Count - 1; i >= 0; i--)
                for (int j = HistoricalData.Count - 1; j >= 0; j--)
                {
                    if (HistoricalData[j] == SelectedHistoricalData[i])
                    {
                        HistoricalData.RemoveAt(j);
                        break;
                    }
                }

                SelectedHistoricalData = null;
            }
        }

        private void btnCreateSimulatedData_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new wndCreateSimulatedData();
            var res = wnd.ShowDialog();

            if(!res.HasValue || !res.Value)
                return;

            if (HistoricalData.Any(p => p.Symbol.Equals(wnd.Symbol) && p.DataFeed.Equals(wnd.DataFeed) &&
                                           p.Periodicity == wnd.Periodicity && p.Interval == wnd.Interval))
            {
                MessageBox.Show("Historical data with same parameters already loaded",
               "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var data = GlobalHelper.CreateSimulated(new SimulatedDataParameters
            {
                Symbol = wnd.Symbol,
                SecurityId = wnd.SecurityID,
                DataFeed = wnd.DataFeed,
                Periodicity = wnd.Periodicity,
                Interval = wnd.Interval,
                BarsCount = wnd.BarsCount,
                TicksCount = wnd.TicksCount,
                MarketLevels = wnd.MarketLevels,
                PriceMax = wnd.PriceMax,
                PriceMin = wnd.PriceMin,
                Slot = wnd.Slot
            });

            ////optional
            //var questionResult = MessageBox.Show("Save simulated data to CSV file?", "Question",
            //    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            //if (questionResult == MessageBoxResult.Yes)
            //{
            //    var saveFileDialog = new SaveFileDialog();
            //    saveFileDialog.Filter = "CSV Files|*.csv";
            //    saveFileDialog.FilterIndex = 1;
            //    saveFileDialog.RestoreDirectory = true;
            //    saveFileDialog.FileName = data.ToString().Replace('/', '-');
            //    if (saveFileDialog.ShowDialog().Value)
            //    {
            //        using (var writer = File.CreateText(saveFileDialog.FileName))
            //            writer.Write(GlobalHelper.CreateCSV(data));
            //    }
            //}

            HistoricalData.Add(data);
        }

        private void btnNewQuote_Click(object sender, RoutedEventArgs e)
        {
            if(ManuallyTickAdded == null || SelectedHistoricalData == null || SelectedHistoricalData.Count == 0)
                return;

            var firstSelectedItem = (HistoricalData)SelectedHistoricalData[0];
            if (firstSelectedItem.Bars.Last().Timestamp >= Date)
                Date = Date.AddMinutes(1);

            var quote = new Quote
            {
                BidPrice = (decimal)Bid,
                AskPrice = (decimal)Ask,
                AskSize = (decimal)AskSize,
                BidSize = (decimal)BidSize,
                Volume = (long)(BidSize + AskSize),
                Time = Date
            };

            ManuallyTickAdded(this, quote);

            var price1 = Bid * GetRandomCoefficient();
            var price2 = Ask * GetRandomCoefficient();
            Bid = Math.Max(price1, price2);
            Ask = Math.Min(price1, price2);
            BidSize = BidSize * GetRandomCoefficient();
            AskSize = AskSize * GetRandomCoefficient();

            var timeSpan = new TimeSpan();
            if (firstSelectedItem.Periodicity == Periodicity.Minute)
                timeSpan = TimeSpan.FromMinutes(firstSelectedItem.Interval / 5.0);
            if (firstSelectedItem.Periodicity == Periodicity.Hour)
                timeSpan = TimeSpan.FromHours(firstSelectedItem.Interval / 5.0);
            if (firstSelectedItem.Periodicity == Periodicity.Day)
                timeSpan = TimeSpan.FromDays(firstSelectedItem.Interval / 5.0);
            if (firstSelectedItem.Periodicity == Periodicity.Month)
                timeSpan = TimeSpan.FromDays(firstSelectedItem.Interval * 30 / 5.0);

            Date = Date + timeSpan;
        }

        private void cmnAddAccount_Click(object sender, RoutedEventArgs e)
        {
            var account = new AccountInfo();
            var wnd = new wndAddBrokerAccount(account);
            var res = wnd.ShowDialog();

            if(!res.HasValue || !res.Value)
                return;

            if (Accounts.Any(p => p.ID.Equals(account.ID)))
            {
                MessageBox.Show("Failed to add broker account, account with same ID already exist.");
                return;
            }

            if (Accounts.Any(p => p.UserName.Equals(account.UserName)))
            {
                MessageBox.Show("Failed to add broker account, account with same User Name already exist.");
                return;
            }

            Accounts.Add(account);
        }

        private void cmnEditAccount_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedAccount == null)
                return;

            var copy = SelectedAccount.Clone() as AccountInfo;
            var wnd = new wndAddBrokerAccount(copy);
            var res = wnd.ShowDialog();

            if (!res.HasValue || !res.Value)
                return;

            if (Accounts.Any(p => p.ID.Equals(copy.ID)) && !SelectedAccount.ID.Equals(copy.ID))
            {
                MessageBox.Show("Failed to update broker, account with same ID already exist.");
                return;
            }

            if (Accounts.Any(p => p.UserName.Equals(copy.UserName)) && !SelectedAccount.UserName.Equals(copy.UserName))
            {
                MessageBox.Show("Failed to update broker, account with same User Name already exist.");
                return;
            }

            Accounts.Remove(SelectedAccount);
            Accounts.Add(copy);
        }

        private void btnRemoveAccount_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedAccount == null)
                return;

            Accounts.Remove(SelectedAccount);

            SelectedAccount = null;
        }

        private void btnRefreshCodeParameters_OnClick(object sender, RoutedEventArgs e)
        {
            RefreshCodeParametersNeeded?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Random coefficient needed for next tick values (manually added ticks)
        /// </summary>
        private double GetRandomCoefficient()
        {
            var rand = (double)_random.Next(80, 120);
            rand = rand / 100;
            return rand;
        }

        public void SetCodeParameters(IEnumerable<CodeParameterBase> parameters)
        {
            Parameters = new ObservableCollection<CodeParameterBase>(parameters);
        }

        private Workspace GetWorkspace()
        {
            return new Workspace
            {
                Bid = Bid,
                Ask = Ask,
                BidSize = BidSize,
                AskSize = AskSize,
                Date = Date,
                Accounts = Accounts.ToList(),
                HistoricalDataCollections = HistoricalData.ToList(),
            };
        }

        private void ApplyWorkspace(Workspace ws)
        {
            if (ws == null)
                return;

            Bid = ws.Bid;
            Ask = ws.Ask;
            BidSize = ws.BidSize;
            AskSize = ws.AskSize;
            Date = ws.Date;

            Accounts.Clear();
            foreach (var item in ws.Accounts)
                Accounts.Add(item);
            if (Accounts.Count > 0)
                SelectedAccount = Accounts[0];

            HistoricalData.Clear();
            foreach (var item in ws.HistoricalDataCollections)
                HistoricalData.Add(item);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
