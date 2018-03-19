using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinpanicLib.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class BlockData
    {
        public string fork { get; set; }
        public List<ICoin> coins { get; set; }
        public string utx { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<string> addresses { get; set; }
    }
}
