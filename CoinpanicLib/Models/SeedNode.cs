using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoinpanicLib.Models
{
    //Information on connecting to the P2P networks
    public class SeedNode
    {
        [Key]
        public int SeedNodeId { get; set; }

        public string Coin { get; set; }

        public string IP { get; set; }

        public int Port { get; set; }

        public bool Enabled { get; set; }
    }
}