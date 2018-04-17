using LightningLib.lndrpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace coinpanic_airdrop.Models
{
    public class LnChannelInfoModel
    {
        public List<LnChannelConnectionPoints> History { get; set; }
        public Channel ChanInfo { get; set; }
        public LnNode RemoteNode { get; set; }
    }

    public class LnStatusViewModel
    {
        public List<LnChannelInfoModel> channels { get; set; }
    }

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