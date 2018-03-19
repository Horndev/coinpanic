using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace coinpanic_airdrop.Models
{
    public class ServicesViewModel
    {
        public List<ForkService> Services { get; set; }
    }

    public class ForkService
    {
        public string Coin { get; set; }
        public List<Peer> Peers {get; set;}
    }

    public class Peer
    {
        public string IP { get; set; }
    }
}