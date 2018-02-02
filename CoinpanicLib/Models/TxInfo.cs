using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CoinpanicLib.Models
{
    /// <summary>
    /// Information about a transaction
    /// </summary>
    public class TxInfo
    {
        /// <summary>
        /// Transaction hash identifier
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Result of transaction
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// If the network returned an error on broadcast, true, otherwise, false.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// True if the transaction was broadcast to the node.
        /// </summary>
        public bool WasBroadcast { get; set; }

        /// <summary>
        /// Hexadecimal encoded transaction
        /// </summary>
        public string Hex { get; set; }
    }
}