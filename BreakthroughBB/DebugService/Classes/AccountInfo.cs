using System;
using System.ComponentModel;
using DebugService.Annotations;

namespace DebugService.Classes
{
    public class AccountInfo : INotifyPropertyChanged, ICloneable
    {
        private string _id;
        private string _currency;
        private string _userName;
        private decimal _balance;

        public string ID
        {
            get => _id;
            set
            {
                if (value == _id) return;
                _id = value;
                OnPropertyChanged("ID");
            }
        }

        public string Currency
        {
            get => _currency;
            set
            {
                if (value == _currency) return;
                _currency = value;
                OnPropertyChanged("Currency");
            }
        }

        public string UserName
        {
            get => _userName;
            set
            {
                if (value == _userName) return;
                _userName = value;
                OnPropertyChanged("UserName");
            }
        }

        public string Password { get; set; }
        
        public string Uri { get; set; }

        public decimal Balance
        {
            get => _balance;
            set
            {
                _balance = value;
                OnPropertyChanged("Balance");
            }
        }

        public decimal Margin { get; set; }
        
        public decimal Equity { get; set; }
        
        public decimal Profit { get; set; }
        
        public CurrencyBasedCoefficient CurrencyBasedCoefficient { get; set; }

        public AccountInfo()
        {
            ID = DateTime.Now.Ticks.ToString();
            Currency = "USD";
            UserName = String.Empty;
            Password = string.Empty;
            Uri = string.Empty;
            CurrencyBasedCoefficient = new CurrencyBasedCoefficient();
        }


        public override bool Equals(object obj)
        {
            if (!(obj is AccountInfo info))
                return false;

            return info.ID.Equals(ID) && info.UserName.Equals(UserName) && info.Password.Equals(Password);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override string ToString()
        {
            return $"{ID} - '{UserName}', {Currency}, '{Balance}'";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}