using System;
using System.ComponentModel;
using System.Windows;
using DebugService.Annotations;
using DebugService.Classes;

namespace DebugService
{
    /// <summary>
    /// Interaction logic for wndAddBrokerAccount.xaml
    /// </summary>
    public partial class wndAddBrokerAccount : INotifyPropertyChanged
    {
        private AccountInfo _account;
        public event PropertyChangedEventHandler PropertyChanged;

        public AccountInfo Account
        {
            get { return _account; }
            set
            {
                if (Equals(value, _account)) return;
                _account = value;
                OnPropertyChanged("Account");
            }
        }

        public wndAddBrokerAccount(AccountInfo account)
        {
            Account = account;
            DataContext = this;
            InitializeComponent();
        }
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Account.ID) ||
                string.IsNullOrEmpty(Account.Currency) ||
                string.IsNullOrEmpty(Account.UserName))
            {
                MessageBox.Show("Account information contain incorrect data.");
                return;
            }

            if (!Account.Currency.Equals("EUR") && !Account.Currency.Equals("USD") && !Account.Currency.Equals("GBP"))
            {
                MessageBox.Show("'EUR','USD','GBP' - is allowed as base currency.");
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
