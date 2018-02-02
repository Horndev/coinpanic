using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinpanicLib.NodeConnection
{
    public class NodesStatus
    {
        public List<NodeStatus> Nodes { get; set; }
    }

    public class NodeStatus
    {
        public string Status { get; set; }
        public string IP { get; set; }
        public string port { get; set; }
        public string name { get; set; }
        public string uptime { get; set; }
        public string version { get; set; }
    }
}
