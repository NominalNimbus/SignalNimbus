using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using DebugService.Annotations;

namespace DebugService.Classes
{
    public class HistoricalData : ICloneable, INotifyPropertyChanged
    {
        private string _symbol;
        private int _securityID;
        private string _dataFeed;
        private Periodicity _periodicity;
        private int _interval;
        private int _slot;

        public string Symbol
        {
            get { return _symbol; }
            set
            {
                if (_symbol != value)
                {
                    _symbol = value;
                    OnPropertyChanged("Symbol");
                }
            }
        }

        public int SecurityID
        {
            get { return _securityID; }
            set
            {
                if (_securityID != value)
                {
                    _securityID = value;
                    OnPropertyChanged("SecurityID");
                }
            }
        }

        public string DataFeed
        {
            get { return _dataFeed; }
            set
            {
                if (_dataFeed != value)
                {
                    _dataFeed = value;
                    OnPropertyChanged("DataFeed");
                }
            }
        }

        public Periodicity Periodicity
        {
            get { return _periodicity; }
            set
            {
                if (_periodicity != value)
                {
                    _periodicity = value;
                    OnPropertyChanged("Periodicity");
                }
            }
        }

        public int Interval
        {
            get { return _interval; }
            set
            {
                if (_interval != value)
                {
                    _interval = value;
                    OnPropertyChanged("Interval");
                }
            }
        }

        public int Slot
        {
            get { return _slot; }
            set
            {
                if (_slot != value)
                {
                    _slot = value;
                    OnPropertyChanged("Slot");
                }
            }
        }

        public string Title
        {
            get
            {
                var p = Periodicity == Periodicity.Month ? "MN" : Periodicity.ToString().Substring(0, 1);
                return String.Format("{0}: {1}{2}, {3} bars{4}", Symbol, p, Interval, Bars.Count,
                    Quotes.Any() ? (", " + Quotes.Count + " quotes") : String.Empty);
            }
        }

        [XmlArray("Bars"), XmlArrayItem("Bar", typeof(Bar))]
        public ObservableCollection<Bar> Bars { get; private set; }

        [XmlArray("Quotes"), XmlArrayItem("Quote", typeof(Quote))]
        public ObservableCollection<Quote> Quotes { get; private set; }

        public HistoricalData()
        {
            Symbol = String.Empty;
            DataFeed = String.Empty;
            Bars = new ObservableCollection<Bar>();
            Quotes = new ObservableCollection<Quote>();
            Bars.CollectionChanged += (s, e) => 
            {
                if (e.NewItems != null && e.NewItems.Count > 0)
                    OnPropertyChanged("Title");
            };
            Quotes.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null && e.NewItems.Count > 0)
                    OnPropertyChanged("Title");
            };
        }

        public object Clone()
        {
            var clone = MemberwiseClone() as HistoricalData;
            clone.Bars = new ObservableCollection<Bar>(Bars.Select(p => p.Clone() as Bar));
            clone.Quotes = new ObservableCollection<Quote>(Quotes.Select(p => p.Clone() as Quote));
            return clone;
        }

        public override bool Equals(object obj)
        {
            var o = obj as HistoricalData;
            return o != null 
                && o.DataFeed == DataFeed 
                && o.Symbol == Symbol 
                && o.SecurityID == SecurityID
                && o.Interval == Interval
                && o.Periodicity == Periodicity;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
