using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NodeInterface.Models
{
    public class IndexViewModel
    {
        public BroadcastModel Broadcast { get; set; }
        public List<PeerModel> Peers { get; set; }
        public PeerModel NewPeer { get; set; }
    }
}