using System;
using System.ComponentModel;
using System.Windows;
using DebugService.Annotations;
using DebugService.Classes;

namespace DebugService
{
    /// <summary>
    /// Interaction logic for wndCreateSimulatedData.xaml
    /// </summary>
    public partial class wndCreateSimulatedData : INotifyPropertyChanged
    {
        private string _symbol;
        private int _securityId;
        private string _dataFeed;
        private Periodicity _periodicity;
        private int _interval;
        private int _barsCount;
        private int _ticksCount;
        private int _marketLevels;
        private double _priceMax;
        private double _priceMin;
        private int _slot;

        public string Symbol
        {
            get { return _symbol; }
            set
            {
                if (value == _symbol) return;
                _symbol = value;
                OnPropertyChanged("Symbol");
            }
        }

        public int SecurityID
        {
            get { return _securityId; }
            set
            {
                if (value == _securityId) return;
                _securityId = value;
                OnPropertyChanged("SecurityID");
            }
        }

        public string DataFeed
        {
            get { return _dataFeed; }
            set
            {
                if (value == _dataFeed) return;
                _dataFeed = value;
                OnPropertyChanged("DataFeed");
            }
        }

        public Periodicity Periodicity
        {
            get { return _periodicity; }
            set
            {
                if (value == _periodicity) return;
                _periodicity = value;
                OnPropertyChanged("Periodicity");
            }
        }

        public int Interval
        {
            get { return _interval; }
            set
            {
                if (value == _interval) return;
                _interval = value;
                OnPropertyChanged("Interval");
            }
        }

        public int BarsCount
        {
            get { return _barsCount; }
            set
            {
                if (value == _barsCount) return;
                _barsCount = value;
                OnPropertyChanged("BarsCount");
            }
        }

        public int TicksCount
        {
            get { return _ticksCount; }
            set
            {
                if (value == _ticksCount) return;
                _ticksCount = value;
                OnPropertyChanged("TicksCount");
            }
        }

        public int MarketLevels
        {
            get { return _marketLevels; }
            set
            {
                if (value == _marketLevels) return;
                _marketLevels = value;
                OnPropertyChanged("MarketLevels");
            }
        }

        public double PriceMax
        {
            get { return _priceMax; }
            set
            {
                if (value.Equals(_priceMax)) return;
                _priceMax = value;
                OnPropertyChanged("PriceMax");
            }
        }

        public double PriceMin
        {
            get { return _priceMin; }
            set
            {
                if (value.Equals(_priceMin)) return;
                _priceMin = value;
                OnPropertyChanged("PriceMin");
            }
        }

        public int Slot
        {
            get { return _slot; }
            set
            {
                if (value != _slot)
                {
                    _slot = value;
                    OnPropertyChanged("Slot");
                }
            }
        }

        public wndCreateSimulatedData()
        {
            Symbol = "EUR/USD";
            SecurityID = 100;
            DataFeed = "LMAX";
            Periodicity = Periodicity.Minute;
            Interval = 1;
            BarsCount = 50;
            TicksCount = 0;
            MarketLevels = 0;
            PriceMax = 1.1;
            PriceMin = 0.9;
            Slot = 1;
            DataContext = this;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(Symbol) || String.IsNullOrEmpty(DataFeed) || PriceMax <= PriceMin)
            {
                MessageBox.Show("Invalid parameters.");
                return;
            }

            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
