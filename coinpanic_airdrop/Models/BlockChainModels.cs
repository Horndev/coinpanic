using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace coinpanic_airdrop.Models
{
    public class AddressSearchViewModel
    {
        public List<string> Coins { get; set; }
        public string Addresses { get; set; }
    }

    public class AddressSummary
    {
        public string Coin { get; set; }
        public string CoinName { get; set; }
        public List<string> Addresses { get; set; }
        public List<string> ConvertedAddress { get; set; }
        public List<string> InvalidAddresses { get; set; }
        public string Balance { get; set; }
        public bool Empty { get; set; }
        public bool UsedExplorer { get; set; }
    }
}