using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NodeInterface.Models
{
    /// <summary>
    /// Information about a peer node.
    /// </summary>
    public class PeerInfo
    {
        /// <summary>
        /// Public ip address
        /// </summary>
        public string ip { get; set; }

        /// <summary>
        /// Port used to connect
        /// </summary>
        public int port { get; set; }
    }
}