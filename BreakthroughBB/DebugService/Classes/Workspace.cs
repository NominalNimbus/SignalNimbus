using System.Collections.Generic;
using System.Xml.Serialization;

namespace DebugService.Classes
{
    public class Workspace
    {
        public double Bid { get; set; }

        public double Ask { get; set; }

        public System.DateTime Date { get; set; }

        public double AskSize { get; set; }

        public double BidSize { get; set; }

        [XmlArray("Accounts"), XmlArrayItem("Account", typeof(AccountInfo))]
        public List<AccountInfo> Accounts { get; set; }

        [XmlArray("HistoricalDataCollections"), XmlArrayItem("HistoricalData", typeof(HistoricalData))]
        public List<HistoricalData> HistoricalDataCollections { get; set; }

        public Workspace()
        {
            Accounts = new List<AccountInfo>();
            HistoricalDataCollections = new List<HistoricalData>();
        }
    }
}
