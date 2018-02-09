using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinpanicLib.NodeConnection.Api
{
    /// <summary>
    /// 
    /// </summary>
    public class BroadcastResponse
    {
        public string Result { get; set; }
        public bool Error { get; set; }
        public string Txid { get; set; }
    }
}
