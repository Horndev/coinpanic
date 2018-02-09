using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NodeInterface.Models
{
    public class PeerModel
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public string IP { get; set; }
        public int port { get; set; }
        public string status { get; set; }
        public string uptime { get; set; }
        public string DateLastConnect { get; set; }
        public string DateLastDisconnect { get; set; }
        public bool IsConnected { get; set; }
    }
}