using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace coinpanic_airdrop.Models
{
    /// <summary>
    /// This is the model for the home-page description of available coins.
    /// </summary>
    public class IndexCoinInfo
    {
        public IndexCoinInfo()
        {
            Exchanges = new List<ExchangeInfo>();
        }

        [Key]
        public int InfoId { get; set; }
        public string Coin { get; set; }
        public string Status { get; set; } // online, degraded, offline
        public int Nodes { get; set; }
        public string CoinName { get; set; }
        public string CoinHeaderMessage { get; set; }
        public string CoinNotice { get; set; }
        public string AlertClass { get; set; }
        public string Exchange { get; set; }
        public string ExchangeURL { get; set; }
        public string ExchangeConfirm { get; set; }
        public string ExplorerURL { get; set; }
        public string ExplorerUsed { get; set; }
        public virtual ICollection<ExchangeInfo> Exchanges { get; set; }
    }

    public class ExchangeInfo
    {
        public ExchangeInfo()
        {
            Coins = new List<IndexCoinInfo>();
        }

        [Key]
        public int ExchangeId { get; set; }

        public string Name { get; set; }
        public string URL { get; set; }
        public string Confirmed { get; set; }
        public int Rating { get; set; }
        public string KYC { get; set; }

        public virtual ICollection<IndexCoinInfo> Coins { get; set; }
    }




    public class IndexModel
    {
        public Dictionary<string, IndexCoinInfo> CoinInfo { get; set; }
    }
}