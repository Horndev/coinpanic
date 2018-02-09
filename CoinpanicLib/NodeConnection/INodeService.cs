using NBitcoin;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinpanicLib.NodeConnection
{
    public interface INodeService
    {
        string Coin { get; set; }

        /// <summary>
        /// Get the number of peers this node is connected to
        /// </summary>
        int NumConnectedPeers { get; }

        NodesStatus GetConnectedPeers { get; }

        TxDetails BroadcastTransaction(Transaction t, bool force);

        void ConnectNodes(List<NodeDetails> seedNodes, int maxnodes = 3);

        Node TryGetNode(string ip, int port);

        // Debugging stuff (delete)
        string Val { get; set; }
        void Test();
    }
}
