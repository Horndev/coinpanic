using CoinpanicLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace coinpanic_airdrop.Models
{
    public class NodesInfo
    {
        public List<NodeServices> NodeServices { get; set; }
        public List<SeedNode> Nodes { get; set; }
    }
}