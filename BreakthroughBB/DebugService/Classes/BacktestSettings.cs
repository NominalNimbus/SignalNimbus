namespace DebugService.Classes
{
    public class BacktestSettings
    {
        public System.DateTime StartDate { get; set; }

        public System.DateTime EndDate { get; set; }

        public int BarsBack { get; set; }

        public decimal InitialBalance { get; set; }

        public decimal Risk { get; set; }

        public decimal TransactionCosts { get; set; }

        public BacktestSettings()
        {

        }
    }
}