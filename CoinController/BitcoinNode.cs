using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using SimpleTCP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoinController
{
    public class NodeDetails
    {
        string ip;                  // Where to connect:  "x.x.x.x:ppp"
        int port;
        int numDisconnects;         // Number of disconnects
        DateTime lastDisconnect;    // When last disconnected (to not spam connect)
        bool use;                   // If false, will not use
    }

    public sealed class CoinPanicServer
    {
        //each coin gets a node server
        private static ConcurrentDictionary<string, NodeServer> _servers = new ConcurrentDictionary<string, NodeServer>();

        //This will contain a collection of seed nodes
        public static ConcurrentDictionary<string, BlockingCollection<NodeDetails>> _seedNodeAddresses = new ConcurrentDictionary<string, BlockingCollection<NodeDetails>>();

        /// <summary>
        /// Event handler for dropped nodes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="node"></param>
        private static void Svr_NodeRemoved(NodeServer sender, Node node)
        {
            Console.WriteLine("disconnect " + node.RemoteSocketEndpoint.Address.ToString());
        }

        public static NodeServer GetNodeServer(string coin, List<NodeDetails> seedNodes)
        {
            NodeServer svr;
            if (!_servers.TryGetValue(coin, out svr))
            {
                //coin not in dictionary
                svr = CreateNodeServer(coin);
                if (!_servers.TryAdd(coin, svr))
                {
                    return null;
                }
            }
            return svr;
        }

        public static int BroadcastTransaction(string coin, Transaction transaction)
        {
            var svr = GetNodeServer(coin);
            string txhash = transaction.GetHash().ToString();
            int numsuccess = svr.ConnectedNodes.Count;

            var connectedNodes = svr.ConnectedNodes.Where(n => n.State == NodeState.HandShaked);

            //broadcast to all connected nodes
            foreach (var n in connectedNodes)
            {
                try
                {
                    n.SendMessageAsync(new InvPayload(transaction));
                    n.SendMessageAsync(new TxPayload(transaction));
                    //n.PingPong();
                }
                catch
                {
                    numsuccess -= 1;
                }
            }
            return numsuccess;
        }

         private static Node ConnectNode(NodeServer svr, string IP, int port)
        {
            var node = svr.FindOrConnect(new IPEndPoint(IPAddress.Parse(IP).MapToIPv6Ex(), port));
            node.VersionHandshake();
            svr.NodeRemoved += Svr_NodeRemoved;
            return node;
        }

        private static NodeServer CreateNodeServer(string coin)
        {
            //This is me 
            string externalip = new WebClient().DownloadString("http://icanhazip.com").Trim('\n');
            var n = Network.Main;
            ProtocolVersion v = ProtocolVersion.SBTC_VERSION;
            NodeConnectionParameters p = new NodeConnectionParameters()
            {
                Services = NodeServices.Nothing,            // Yeah, I'm a leech
                UserAgent = "coinpanic.com",                // The definition of middleman
                IsRelay = false,                            // Just, don't talk to me
                Version = ProtocolVersion.SBTC_VERSION,     // I hack it
            };

            //This is to accept incomming connections
            //AddressManagerBehavior behavior = new AddressManagerBehavior(new AddressManager());
            //p.TemplateBehaviors.Add(behavior);

            if (coin == "BCD")
            {
                //debug this
                //n.Magic = ;
                n.DefaultPort = 7117;
            }
            else if (coin == "BTF")
            {
                //debug this
                n.Magic = 0xE6D4E2FA;
                n.DefaultPort = 8346;
            }
            else if (coin == "SBTC")
            {
                n.DefaultPort = 8334;
                n.Magic = 0xD9B4BEF9;
            }
            else if (coin == "BCX")
            {
                //magic for Bitcoin X
                n.Magic = 0xF9BC0511;
                n.DefaultPort = 9003;
            }
            
            var svr = new NodeServer(n, v);
            svr.InboundNodeConnectionParameters = p;
            svr.ExternalEndpoint = new IPEndPoint(IPAddress.Parse(externalip).MapToIPv6Ex(), n.DefaultPort);
            svr.LocalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6Ex(), n.DefaultPort);
            svr.Listen();
            return svr;
        }
    }


    /// <summary>
    /// This will contain the singletons for nodes
    /// </summary>
    public sealed class CoinPanicNodes
    {
        //Stores access to nodes.
        private static ConcurrentDictionary<string, List<Node>> _nodes = new ConcurrentDictionary<string, List<Node>>();

        public static object txlock = new object();
        public static IList<Node> GetNodes(string coin)
        {
            List<Node> coinNodes = null;

            try
            {
                if (!_nodes.TryGetValue(coin, out coinNodes))
                {
                    //first time called
                    coinNodes = launchNodes(coin);
                    if (!_nodes.TryAdd(coin, coinNodes))
                    {
                        //coinNodes.ForEach(n => n.Disconnect());
                        //throw new Exception("unable to launch node");
                    }
                }
                if (coinNodes == null || (coinNodes != null && coinNodes.Count < 1))
                {
                    if (!_nodes.TryRemove(coin, out coinNodes))
                    {
                        lock (txlock)
                        {
                            coinNodes = launchNodes(coin);
                        }
                        if (!_nodes.TryAdd(coin, coinNodes))
                        {
                            //coinNodes.ForEach(n => n.Disconnect());
                            //throw new Exception("unable to launch node");
                        }
                    }
                }
                //Verify connectivity
                List<Node> checkedCoinNodes = new List<Node>();
                if (coinNodes == null)
                {
                    return checkedCoinNodes;
                }
                //foreach (var coinNode in coinNodes)
                //{
                //    if ( 
                //        (coinNode != null) && ( (coinNode.State != NodeState.HandShaked) ) 
                //        )
                //    {
                //        if (coinNode != null)
                //        {
                //            coinNode.DisconnectAsync();
                //        }
                //        //if the node has failed.  Try reconnect
                //        Debug.WriteLine(coinNode.DisconnectReason);
                //        var n = Network.Main;
                //        n.Magic = coinNode.Network.Magic;
                //        lock (txlock)
                //        {
                //            var newcoinNode = launchNode(coin, coinNode.RemoteSocketEndpoint.Address.ToString() + ":" + Convert.ToString(coinNode.Peer.Endpoint.Port), n);    //new connection
                //            coinNode.Dispose();
                //            checkedCoinNodes.Add(newcoinNode);
                //        }
                //    }
                //    else
                //    {
                //        checkedCoinNodes.Add(coinNode);
                //    }
                //}
                //_nodes.TryRemove(coin, out coinNodes);
                //_nodes.TryAdd(coin, checkedCoinNodes);
                //coinNodes = checkedCoinNodes;
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine(e.Message);
            }

            if (coinNodes != null)
            {
                return coinNodes;
            }
            throw new KeyNotFoundException("Node " + coin + " not found and was not able to be launched.");
        }

        private static Node launchNode(string coin, string endpoint, Network n)
        {
            try
            {
                IPAddress address = Dns.GetHostAddresses("www.coinpanic.com")[0];
                var node = Node.Connect(n, endpoint, new NodeConnectionParameters()
                {
                    Services = NodeServices.Nothing,
                    UserAgent = "coinpanic.com",
                    IsRelay = false,
                    //AddressFrom = new IPEndPoint(address, n.DefaultPort),
                    Version = ProtocolVersion.SBTC_VERSION,//forkCode == Forks.ForkCode.SBTC ? ProtocolVersion.SBTC_VERSION :
                                                           //(forkCode == Forks.ForkCode.BTF ? ProtocolVersion.BTF_VERSION :
                                                           //(forkCode == Forks.ForkCode.BCX ? ProtocolVersion.SBTC_VERSION : ProtocolVersion.WITNESS_VERSION))
                });
                
                node.VersionHandshake();
                Debug.WriteLine(endpoint + ": Handshake ok.");
                return node;
            }
            catch
            {
                return null;
            }
        }

        private static List<Node> launchNodes(string coin)
        {
            List<Node> coinNodes = new List<Node>();
            List<string> nodeEndpoints = new List<string>();

            var n = Network.Main;
            if (coin == "BCD")
            {
                //debug this
                //n.Magic = ;
                n.DefaultPort = 7117;
                nodeEndpoints.Add("127.0.0.1:7117");
                nodeEndpoints.Add("192.168.56.101:7117");
                nodeEndpoints.Add("47.90.37.123:7117");
            }
            else if (coin == "BTF")
            {
                //debug this
                n.Magic = 0xE6D4E2FA;
                n.DefaultPort = 8346;
                nodeEndpoints.Add("127.0.0.1:8346");
                nodeEndpoints.Add("47.90.37.123:8346");
            }
            else if (coin == "SBTC")
            {
                n.DefaultPort = 8334;
                n.Magic = 0xD9B4BEF9;
                nodeEndpoints.Add("127.0.0.1:8334");    //if there is a node on the host
                nodeEndpoints.Add("59.110.10.92:8334");
                nodeEndpoints.Add("101.201.117.68:8334");
                nodeEndpoints.Add("39.104.28.97:8334");
                nodeEndpoints.Add("78.199.168.201:8334");
                nodeEndpoints.Add("120.78.188.194:8334");
                nodeEndpoints.Add("136.243.147.159:8334");
                nodeEndpoints.Add("185.17.31.58:8334");
                nodeEndpoints.Add("162.212.157.232:8334");
                nodeEndpoints.Add("101.201.117.68:8334");
                nodeEndpoints.Add("162.212.157.232:8334");
                nodeEndpoints.Add("123.56.143.216:8334");
            }
            else if (coin == "BCX")
            {
                //magic for Bitcoin X
                n.Magic = 0xF9BC0511;
                n.DefaultPort = 9003;
                nodeEndpoints.Add("127.0.0.1:9003");    //if there is a node on the host
                nodeEndpoints.Add("142.166.17.89:9003");    //me
                nodeEndpoints.Add("120.131.5.173:9003");
                nodeEndpoints.Add("120.92.117.145:9003");
                nodeEndpoints.Add("192.169.153.174:9003");
                nodeEndpoints.Add("192.169.154.185:9003");
                nodeEndpoints.Add("166.62.117.163:9003");
                nodeEndpoints.Add("192.169.227.48:9003");
                nodeEndpoints.Add("162.212.156.23:9003");
                nodeEndpoints.Add("172.104.42.222:9003");
                nodeEndpoints.Add("185.12.237.78:9003");
                nodeEndpoints.Add("120.92.119.221:9003");
                nodeEndpoints.Add("120.131.7.70:9003");
                nodeEndpoints.Add("120.92.89.254:9003");

            }
            foreach(string endpoint in nodeEndpoints)
            {
                var ln = launchNode(coin, endpoint, n);
                if (ln != null)
                {
                    coinNodes.Add(ln);
                }
            }
            return coinNodes;
        }

        /// <summary>
        /// Broadcast a transaction to nodes for a coin
        /// </summary>
        /// <param name="coin"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static int BroadcastTransaction(string coin, Transaction transaction)
        {
            //Monitor.Enter(txlock);
            //try { 
                var nodes = GetNodes(coin); //Get the nodes for this coin
                string txhash = transaction.GetHash().ToString();
                int numsuccess = nodes.Count;
                //broadcast to all connected nodes
                foreach (var n in nodes)
                {
                    try
                    {
                        n.SendMessageAsync(new InvPayload(transaction));
                        n.SendMessageAsync(new TxPayload(transaction));
                        n.PingPong();
                    }
                    catch
                    {
                        numsuccess -= 1;
                    }
                }
                return numsuccess;
            //}
            //finally
            //{
            //    Monitor.Exit(txlock);
            //}
            
        }

        //static void AttachedNode_MessageReceived(Node node, IncomingMessage message)
        //{
        //    Debug.Write(message.Message.Command + "  : ");
        //    Debug.WriteLine(message.Message.Payload.ToString());

        //    if (message.Message.Command == "reject")
        //    {
        //        var rejectmessage = (RejectPayload)message.Message.Payload;
        //        response = "Reject message: " + rejectmessage.Message + " Reject reason : " + rejectmessage.Reason + " Reject code   : " + rejectmessage.Code.ToString();
        //        //Console.WriteLine("Reject message: " + rejectmessage.Message);
        //        //Console.WriteLine("Reject reason : " + rejectmessage.Reason);
        //        //Console.WriteLine("Reject code   : " + rejectmessage.Code.ToString());
        //        done = true;
        //    }
        //}
    }



    public class BitcoinNode
    {
        string _address;
        int _port;
        static bool done = false;
        static string response = "";

        public BitcoinNode(string address, int port)
        {
            _address = address;
            _port = port;
        }

        public string BroadcastTransaction(Transaction t, Forks.ForkCode forkCode)
        {
            done = false;
            TxPayload tx = new TxPayload(t);
            string txhash = t.GetHash().ToString();
            response = txhash;
            var n = Network.Main;
            if (forkCode == Forks.ForkCode.BTF)
            { 
                n.Magic = 0xE6D4E2FA;
            }
            else if (forkCode == Forks.ForkCode.BCX)
            {
                n.Magic = 0xF9BC0511;
            }
            using (var node = Node.Connect(n, _address + ":" + _port, new NodeConnectionParameters()
            {
                Services = 0,
                UserAgent = "Coinpanic",
                IsRelay = false,
                Version = ProtocolVersion.SBTC_VERSION,//forkCode == Forks.ForkCode.SBTC ? ProtocolVersion.SBTC_VERSION :
                          //(forkCode == Forks.ForkCode.BTF ? ProtocolVersion.BTF_VERSION :
                          //(forkCode == Forks.ForkCode.BCX ? ProtocolVersion.SBTC_VERSION : ProtocolVersion.WITNESS_VERSION))
            }))
            {
                node.MessageReceived += AttachedNode_MessageReceived;
                node.VersionHandshake();
                Debug.WriteLine("Handshake ok.");
                if (node.State == NodeState.Connected)
                    Debug.WriteLine("Node is Connected.");
                Thread.Sleep(1000);
                Debug.WriteLine("Transmitting.");
                node.SendMessageAsync(new InvPayload(t));
                node.SendMessageAsync(new TxPayload(t));
                node.PingPong();
                Thread.Sleep(1000); //Give some time for a response
            }
            
            return response;
        }
        
        static void AttachedNode_MessageReceived(Node node, IncomingMessage message)
        {
            Debug.Write(message.Message.Command + "  : ");
            Debug.WriteLine(message.Message.Payload.ToString());

            if (message.Message.Command == "reject")
            {
                var rejectmessage = (RejectPayload)message.Message.Payload;
                response = "Reject message: " + rejectmessage.Message + " Reject reason : " + rejectmessage.Reason + " Reject code   : " + rejectmessage.Code.ToString();
                //Console.WriteLine("Reject message: " + rejectmessage.Message);
                //Console.WriteLine("Reject reason : " + rejectmessage.Reason);
                //Console.WriteLine("Reject code   : " + rejectmessage.Code.ToString());
                done = true;
            }

            //InvPayload invPayload = message.Message.Payload as InvPayload;
            //if (invPayload != null)
            //{
            //    done = true;
            //    //Console.WriteLine("Transaction accepted.");
            //}
        }
    }

}

/*
Console.WriteLine(t.GetHash().ToString());
			string nodeip = "185.17.31.58";
			//string nodeip = "127.0.0.1";

			//see https://github.com/MetacoSA/NBitcoin/blob/9f9214a469ffecfd3ce872998d740b108cc56d80/NBitcoin.Tests/Benchmark.cs
			using (var node = Node.Connect(Network.Main, new IPEndPoint(IPAddress.Parse(nodeip), 8334), new NodeConnectionParameters()
			{
				Services = 0,
				UserAgent = "Coinpanic",
				IsRelay = false,
				Version = ProtocolVersion.SBTC_VERSION
			}))
			{
				node.MessageReceived += AttachedNode_MessageReceived;
				Stopwatch sw = new Stopwatch();
				sw.Start();

				while (node.State != NodeState.Connected)
				{
					Thread.Sleep(100);
					if (sw.ElapsedMilliseconds > 5000) throw new TimeoutException();
				}
				var originalNode = node;
				node.VersionHandshake();
				
				Thread.Sleep(100);
				node.SendMessageAsync(new InvPayload(t));
				node.SendMessageAsync(new TxPayload(t));
				node.PingPong();
				//node.SendMessageAsync(new InvPayload(InventoryType.MSG_TX, t.GetHash())).ConfigureAwait(false);
			}
			Thread.Sleep(1000);
			Console.WriteLine("running...");
			Stopwatch sw2 = new Stopwatch();
			sw2.Start();
			while (!done)
			{
				Thread.Sleep(1);
				if (sw2.ElapsedMilliseconds > 5000)
					done = true;
			}
			
			return;
*/
