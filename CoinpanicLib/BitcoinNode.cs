using CoinpanicLib.NodeConnection;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Forks;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoinController
{
    //public class NodeDetails
    //{
    //    public string coin;                 //which coin
    //    public string ip;                  // Where to connect:  "x.x.x.x:ppp"
    //    public int port;
    //    public int numDisconnects;         // Number of disconnects
    //    public DateTime lastDisconnect;    // When last disconnected (to not spam connect)
    //    public bool use;                   // If false, will not use
    //}

    public class TxDetails
    {
        public string Result;
        public string Coin;
        public bool IsError;
        public bool IsMined;
        public bool IsInput;
        public Transaction tx;
        public Node n;
    }

    public sealed class CoinPanicServer
    {
        //each coin gets a node server
        private static ConcurrentDictionary<string, NodeServer> _servers = new ConcurrentDictionary<string, NodeServer>();

        //This will contain a collection of seed nodes
        public static ConcurrentDictionary<string, BlockingCollection<NodeDetails>> _seedNodeAddresses = new ConcurrentDictionary<string, BlockingCollection<NodeDetails>>();

        //our mempool (move to db?)
        public static ConcurrentDictionary<string, TxDetails> txSent = new ConcurrentDictionary<string, TxDetails>();
        public static ConcurrentDictionary<string, TxDetails> txRecv = new ConcurrentDictionary<string, TxDetails>();

        public static bool IsInitialized = false;

        /// <summary>
        /// Event handler for dropped nodes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="node"></param>
        private static void Svr_NodeRemoved(NodeServer sender, Node node)
        {
            //SendMail("disconnected " + node.RemoteSocketEndpoint.Address.ToString(), node.RemoteSocketEndpoint.Address.ToString());
            Debug.WriteLine("disconnect " + node.RemoteSocketEndpoint.Address.ToString());
        }

        public static string emailhost;
        public static int emailport;
        public static string emailuser;
        public static string emailpass;

        private static void Svr_MessageReceived(NodeServer sender, IncomingMessage message)
        {
            Debug.Write(message.Node.Peer.Endpoint.Address.ToString() +":" + message.Message.Command + "  : ");
            Debug.WriteLine(message.Message.Payload.ToString());

            if (message.Message.Command == "notfound")
            {
                var data = (NotFoundPayload)message.Message.Payload;
                foreach (var i in data)
                {
                    //if (i.Hash == txhash) //received message
                    Debug.WriteLine(i.Type.ToString());
                    Debug.WriteLine(i.Hash.ToString());
                    if (i.Type == InventoryType.MSG_TX)
                    {
                        //Received a transaction
                        if (txSent.ContainsKey(i.Hash.ToString()))
                        {
                            if (txSent.TryRemove(i.Hash.ToString(), out TxDetails txInfo))
                            {
                                txInfo.Result = "Transaction was broadcast to the network, but confirmation was not received.  Check your balance.";
                                txInfo.IsError = true;

                                //add it to the list of completed transactions
                                txRecv.TryAdd(i.Hash.ToString(), txInfo);
                            }
                        }
                    }
                }
            }
            else if (message.Message.Command == "reject")
            {
                var rejectmessage = (RejectPayload)message.Message.Payload;
                Debug.WriteLine("Reject message: " + rejectmessage.Message);
                Debug.WriteLine("Reject reason : " + rejectmessage.Reason);
                Debug.WriteLine("Reject code   : " + rejectmessage.Code.ToString());

                var subject = "transaction rejected";
                var messagetx = "Reject message: " + rejectmessage.Message + "Reject reason : " + rejectmessage.Reason + "Reject code   : " + rejectmessage.Code.ToString();

                if (txSent.ContainsKey(rejectmessage.Hash.ToString()))
                {
                    //We sent this
                    if (txSent.TryRemove(rejectmessage.Hash.ToString(), out TxDetails txInfo))
                    {
                        txInfo.Result = "Rejected: " + rejectmessage.Message + ", " + rejectmessage.Reason + " Code " + rejectmessage.Code.ToString();
                        txInfo.IsError = true;
                        //add it to the list of completed transactions
                        txRecv.TryAdd(rejectmessage.Hash.ToString(), txInfo);
                    }
                }
                else
                {
                    Debug.WriteLine("Rejected tx not found in mempool.");
                }
                if (txRecv.ContainsKey(rejectmessage.Hash.ToString()))
                {
                    Debug.WriteLine("We already got a response for " + rejectmessage.Hash.ToString());
                }

                SendMail(subject, messagetx);
            }
            else if (message.Message.Command == "addr")
            {
                var addrmessage = (AddrPayload)message.Message.Payload;
                Debug.WriteLine("Recieved addresses: ");
                foreach (var a in addrmessage.Addresses)
                {
                    Debug.WriteLine(a.Endpoint.Address.ToString());
                }
            }
            else if (message.Message.Command == "feefilter")
            {

            }
            else if (message.Message.Command == "inv")
            {
                // Sent when 
                var data = (InvPayload)message.Message.Payload;
                foreach (var i in data)
                {
                    //if (i.Hash == txhash) //received message
                    Debug.WriteLine(i.Type.ToString());
                    Debug.WriteLine(i.Hash.ToString());
                    if (i.Type == InventoryType.MSG_TX)
                    {
                        //Received a transaction
                        if (txSent.ContainsKey(i.Hash.ToString()))
                        {
                            //We sent this
                            if (txSent.TryRemove(i.Hash.ToString(), out TxDetails txInfo))
                            {
                                txInfo.Result = "Success";
                                txInfo.IsError = false;

                                //add it to the list of completed transactions
                                txRecv.TryAdd(i.Hash.ToString(), txInfo);
                            }
                        }
                        if (txRecv.ContainsKey(i.Hash.ToString()))
                        {
                            Debug.WriteLine("We already got a response for " + i.Hash.ToString());
                        }
                    }
                }
            }
            else if (message.Message.Command == "sendcmpct")
            {

            }
            else if (message.Message.Command == "getdata")
            {
                var h = new HexEncoder();
                var data = (GetDataPayload)message.Message.Payload;
                foreach (var i in data.Inventory)
                {
                    Debug.WriteLine(i.Type.ToString());
                    Debug.WriteLine(i.Hash.ToString());
                    if (i.Type == InventoryType.MSG_TX)
                    {
                        //Received a transaction
                        if (txSent.ContainsKey(i.Hash.ToString()))
                        {
                            //They are asking to see our transaction.  Show them our papers.
                            if (txSent.TryGetValue(i.Hash.ToString(), out TxDetails txInfo))
                            {
                                Debug.WriteLine("Rebroadcast transaction by request.");
                                txInfo.n.SendMessageAsync(new TxPayload(txInfo.tx));
                            }

                        }
                        else
                        {
                            Debug.WriteLine("Sending notfound");
                            message.Node.SendMessageAsync(new NotFoundPayload(i));
                        }
                    }
                }

            }
        }

        public static int GetNumNodes(string coin)
        {
            var ns = GetNodeServer(coin);

            if (ns == null)
                return 0;
            return ns.ConnectedNodes.Count;
        }

        private static void SendMail(string subject, string messagetx)
        {
            var mailmessage = new MailMessage();
            mailmessage.To.Add(new MailAddress("claims@coinpanic.com"));
            mailmessage.From = new MailAddress(emailuser);  // replace with valid value
            mailmessage.Subject = "Monitoring Message: " + subject;
            mailmessage.Body = messagetx;
            mailmessage.IsBodyHtml = false;

            using (var smtp = new SmtpClient())
            {
                var credential = new NetworkCredential
                {
                    UserName = emailuser,
                    Password = emailpass
                };
                smtp.Credentials = credential;
                smtp.Host = emailhost;
                smtp.Port = emailport;
                smtp.EnableSsl = false;
                smtp.Send(mailmessage);
            }
        }

        public static void InitializeNodes(List<NodeDetails> seedNodes, int maxnodes = 3)
        {
            if (seedNodes == null)
                throw new ArgumentException("No seed nodes provided to Initialize nodes.");

            var coins = seedNodes.Select(n => n.coin)
                //.Where(c => c == "B2X") //debugging
                .Distinct().ToList();

            foreach (string coin in coins)
            {
                bool newserver = false;
                NodeServer svr;
                if (!_servers.TryGetValue(coin, out svr))
                {
                    svr = CreateNewNodeServer(coin);
                    newserver = true;
                }

                int numconnected = 0;
                foreach (var sn in seedNodes.Where(c => c.coin == coin))
                {
                    //coinNodeDetail = new BlockingCollection<NodeDetails>();
                    if (sn.use && numconnected <= maxnodes)
                    {
                        bool failed = false;
                        try
                        {
                            if (sn.ip.Contains("coinpanic"))
                            {
                                int z = 1;
                            }
                            var endpoint = Dns.GetHostEntry(sn.ip).AddressList[0];
                            Debug.WriteLine("Connecting (" + coin + ") to " + endpoint + ":" + Convert.ToString(sn.port));
                            var node = svr.FindOrConnect(new IPEndPoint(endpoint.MapToIPv6Ex(), sn.port));
                            Thread.Sleep(0); //Don't underestimate thread preemption... without that the tests crash in mono proc... :(
                            Thread.Sleep(300);
                            if (!node.IsConnected)
                            {
                                Debug.WriteLine("node connection failed");
                            }
                            if (node.State != NodeState.HandShaked)
                            {
                                Debug.WriteLine("Handshaking (" + coin + ") to " + sn.ip + ":" + Convert.ToString(sn.port));
                                node.VersionHandshake(
                                    new NodeRequirement()
                                    {
                                        MinVersion = ProtocolVersion.INIT_PROTO_VERSION,
                                        RequiredServices = NodeServices.Nothing,
                                        SupportSPV = false,
                                    }
                                );
                                numconnected += 1;
                            }
                        }
                        catch (Exception e)
                        {
                            numconnected -= 1;
                            Debug.WriteLine("Failed Connecting (" + coin + ") to " + sn.ip + ":" + Convert.ToString(sn.port) + " " + e.Message);
                            failed = true;
                        }
                    }
                }

                if (newserver && !_servers.TryAdd(coin, svr))
                {
                    throw new Exception("Error saving NodeServer");
                }
            }

            IsInitialized = true;
        }

        public static NodeServer GetNodeServer(string coin)
        {
            NodeServer svr;
            if (!_servers.TryGetValue(coin, out svr))
            {
                //coin not in 
                return null;
            }
            return svr;
        }

        public static AddressManager GetCachedAddrMan(string file)
        {
            if (File.Exists(file))
            {
                return AddressManager.LoadPeerFile(file);
            }
            return new AddressManager();
        }

        public static TxDetails BroadcastTransaction(string coin, Transaction transaction)
        {
            var svr = GetNodeServer(coin);
            string txhash = transaction.GetHash().ToString();
            var connectedNodes = svr.ConnectedNodes.Where(n => n.State == NodeState.HandShaked);
            bool wasreceived = false;
            TxDetails result = null;

            //broadcast until response.
            foreach (var n in connectedNodes)
            {
                try
                {
                    n.SendMessageAsync(new InvPayload(transaction));
                    n.SendMessageAsync(new TxPayload(transaction));
                    n.PingPong();
                    //save prevouts (returned if already spent)
                    //foreach (var ph in transaction.Inputs)
                    //{
                    //    txSent.TryAdd(ph.PrevOut.Hash.ToString(), new TxDetails()
                    //    {
                    //        Coin = coin,
                    //        IsError = false,
                    //        IsMined = false,
                    //        Result = "Already Spent"
                    //    });
                    //}
                    if (!txSent.TryAdd(txhash, new TxDetails()
                    {
                        Coin = coin,
                        IsError = false,
                        IsMined = false,
                        Result = "Transmitted",
                        IsInput = false,
                        tx = transaction,
                    }))
                    {
                        //already in there
                        return new TxDetails()
                        {
                            Coin = coin,
                            IsError = true,
                            IsMined = false,
                            Result = "Transaction in broadcast queue.  No errors returned."
                        };
                    }

                    //wait for response
                    var sw = Stopwatch.StartNew();
                    sw.Start();
                    TxDetails response = null;
                    while(sw.ElapsedMilliseconds < 10000 && !txRecv.TryGetValue(txhash, out response))
                    {
                        Thread.Sleep(100);  //wait for it.
                    }
                    if (response != null)
                    {
                        return response;
                    }
                    else
                    {
                        //timed out - ask if it was received
                        n.SendMessageAsync(new GetDataPayload(new InventoryVector(InventoryType.MSG_TX, transaction.GetHash())));
                        sw.Restart();
                        while (sw.ElapsedMilliseconds < 5000 && !txRecv.TryGetValue(txhash, out response))
                        {
                            Thread.Sleep(100);  //wait for it.
                        }
                        if (response != null)
                        {
                            return response;
                        }
                        else
                        {
                            //not this time
                        }
                    }
                }
                catch
                {
                    txSent.TryRemove(txhash, out result); //Remove if it was there
                    Debug.WriteLine("Broadcast failed " + txhash);
                }
            }
            if (!wasreceived)
            {
                txSent.TryRemove(txhash, out result); //Remove if it was there
                result = new TxDetails()
                {
                    Coin = coin,
                    IsError = true,
                    IsMined = false,
                    Result = "Your transaction was broadcast, but the network did not confirm if the transaction was accepted."
                };
            }
            return result;
        }

        private static NodeServer CreateNewNodeServer(string coin)
        {
            //This is me 
            string externalip = new WebClient().DownloadString("http://icanhazip.com").Trim('\n');
            
            NodeConnectionParameters p = new NodeConnectionParameters()
            {
                Services = NodeServices.Nothing,            // Yeah, I'm a leech
                UserAgent = "coinpanic.com",                // The definition of middleman
                IsRelay = false,                            // Just, don't talk to me
                Version = BitcoinForks.ForkByShortName[coin].ProtocolVersion,//coin == "BCD" ? ProtocolVersion.SHORT_IDS_BLOCKS_VERSION_NOBAN : ProtocolVersion.FORK_VERSION,     // I hack it
            };

            //This is to accept incomming connections
            //AddressManagerBehavior behavior = new AddressManagerBehavior(new AddressManager());
            //p.TemplateBehaviors.Add(behavior);
            Network n = BitcoinForks.ForkByShortName[coin].Network;
            var svr = new NodeServer(n);
            try
            {
                
                svr.NodeRemoved += Svr_NodeRemoved;
                svr.MessageReceived += Svr_MessageReceived;
                svr.InboundNodeConnectionParameters = p;
                svr.AllowLocalPeers = true;
                svr.ExternalEndpoint = new IPEndPoint(IPAddress.Parse(externalip).MapToIPv6Ex(), n.DefaultPort);
                svr.LocalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6Ex(), n.DefaultPort);
                svr.Listen();
            }
            catch (Exception e)
            {
                SendMail("error starting server " + coin, e.Message + "\r\n" + e.StackTrace);
            }
            return svr;
        }
    }


    ///// <summary>
    ///// This will contain the singletons for nodes
    ///// </summary>
    //public sealed class CoinPanicNodes
    //{
    //    //Stores access to nodes.
    //    private static ConcurrentDictionary<string, List<Node>> _nodes = new ConcurrentDictionary<string, List<Node>>();

    //    public static object txlock = new object();
    //    public static IList<Node> GetNodes(string coin)
    //    {
    //        List<Node> coinNodes = null;

    //        try
    //        {
    //            if (!_nodes.TryGetValue(coin, out coinNodes))
    //            {
    //                //first time called
    //                coinNodes = launchNodes(coin);
    //                if (!_nodes.TryAdd(coin, coinNodes))
    //                {
    //                    //coinNodes.ForEach(n => n.Disconnect());
    //                    //throw new Exception("unable to launch node");
    //                }
    //            }
    //            if (coinNodes == null || (coinNodes != null && coinNodes.Count < 1))
    //            {
    //                if (!_nodes.TryRemove(coin, out coinNodes))
    //                {
    //                    lock (txlock)
    //                    {
    //                        coinNodes = launchNodes(coin);
    //                    }
    //                    if (!_nodes.TryAdd(coin, coinNodes))
    //                    {
    //                        //coinNodes.ForEach(n => n.Disconnect());
    //                        //throw new Exception("unable to launch node");
    //                    }
    //                }
    //            }
    //            //Verify connectivity
    //            List<Node> checkedCoinNodes = new List<Node>();
    //            if (coinNodes == null)
    //            {
    //                return checkedCoinNodes;
    //            }
    //            //foreach (var coinNode in coinNodes)
    //            //{
    //            //    if ( 
    //            //        (coinNode != null) && ( (coinNode.State != NodeState.HandShaked) ) 
    //            //        )
    //            //    {
    //            //        if (coinNode != null)
    //            //        {
    //            //            coinNode.DisconnectAsync();
    //            //        }
    //            //        //if the node has failed.  Try reconnect
    //            //        Debug.WriteLine(coinNode.DisconnectReason);
    //            //        var n = Network.Main;
    //            //        n.Magic = coinNode.Network.Magic;
    //            //        lock (txlock)
    //            //        {
    //            //            var newcoinNode = launchNode(coin, coinNode.RemoteSocketEndpoint.Address.ToString() + ":" + Convert.ToString(coinNode.Peer.Endpoint.Port), n);    //new connection
    //            //            coinNode.Dispose();
    //            //            checkedCoinNodes.Add(newcoinNode);
    //            //        }
    //            //    }
    //            //    else
    //            //    {
    //            //        checkedCoinNodes.Add(coinNode);
    //            //    }
    //            //}
    //            //_nodes.TryRemove(coin, out coinNodes);
    //            //_nodes.TryAdd(coin, checkedCoinNodes);
    //            //coinNodes = checkedCoinNodes;
    //        }
    //        catch (ArgumentException e)
    //        {
    //            Debug.WriteLine(e.Message);
    //        }

    //        if (coinNodes != null)
    //        {
    //            return coinNodes;
    //        }
    //        throw new KeyNotFoundException("Node " + coin + " not found and was not able to be launched.");
    //    }

    //    private static Node launchNode(string coin, string endpoint, Network n)
    //    {
    //        try
    //        {
    //            IPAddress address = Dns.GetHostAddresses("www.coinpanic.com")[0];
    //            var node = Node.Connect(n, endpoint, new NodeConnectionParameters()
    //            {
    //                Services = NodeServices.Nothing,
    //                UserAgent = "coinpanic.com",
    //                IsRelay = false,
    //                //AddressFrom = new IPEndPoint(address, n.DefaultPort),
    //                Version = ProtocolVersion.SHORT_IDS_BLOCKS_VERSION_NOBAN,//forkCode == Forks.ForkCode.SBTC ? ProtocolVersion.SBTC_VERSION :
    //                                                       //(forkCode == Forks.ForkCode.BTF ? ProtocolVersion.BTF_VERSION :
    //                                                       //(forkCode == Forks.ForkCode.BCX ? ProtocolVersion.SBTC_VERSION : ProtocolVersion.WITNESS_VERSION))
    //            });
                
    //            node.VersionHandshake();
    //            Debug.WriteLine(endpoint + ": Handshake ok.");
    //            return node;
    //        }
    //        catch
    //        {
    //            return null;
    //        }
    //    }

    //    private static List<Node> launchNodes(string coin)
    //    {
    //        List<Node> coinNodes = new List<Node>();
    //        List<string> nodeEndpoints = new List<string>();

    //        var n = Network.Main;
    //        if (coin == "BCD")
    //        {
    //            //debug this
    //            //n.Magic = ;
    //            n.DefaultPort = 7117;
    //            nodeEndpoints.Add("127.0.0.1:7117");
    //            nodeEndpoints.Add("192.168.56.101:7117");
    //            nodeEndpoints.Add("47.90.37.123:7117");
    //        }
    //        else if (coin == "BTF")
    //        {
    //            //debug this
    //            n.Magic = 0xE6D4E2FA;
    //            n.DefaultPort = 8346;
    //            nodeEndpoints.Add("127.0.0.1:8346");
    //            nodeEndpoints.Add("47.90.37.123:8346");
    //        }
    //        else if (coin == "SBTC")
    //        {
    //            n.DefaultPort = 8334;
    //            n.Magic = 0xD9B4BEF9;
    //            nodeEndpoints.Add("127.0.0.1:8334");    //if there is a node on the host
    //            nodeEndpoints.Add("59.110.10.92:8334");
    //            nodeEndpoints.Add("101.201.117.68:8334");
    //            nodeEndpoints.Add("39.104.28.97:8334");
    //            nodeEndpoints.Add("78.199.168.201:8334");
    //            nodeEndpoints.Add("120.78.188.194:8334");
    //            nodeEndpoints.Add("136.243.147.159:8334");
    //            nodeEndpoints.Add("185.17.31.58:8334");
    //            nodeEndpoints.Add("162.212.157.232:8334");
    //            nodeEndpoints.Add("101.201.117.68:8334");
    //            nodeEndpoints.Add("162.212.157.232:8334");
    //            nodeEndpoints.Add("123.56.143.216:8334");
    //        }
    //        else if (coin == "BCX")
    //        {
    //            //magic for Bitcoin X
    //            n.Magic = 0xF9BC0511;
    //            n.DefaultPort = 9003;
    //            nodeEndpoints.Add("127.0.0.1:9003");    //if there is a node on the host
    //            nodeEndpoints.Add("142.166.17.89:9003");    //me
    //            nodeEndpoints.Add("120.131.5.173:9003");
    //            nodeEndpoints.Add("120.92.117.145:9003");
    //            nodeEndpoints.Add("192.169.153.174:9003");
    //            nodeEndpoints.Add("192.169.154.185:9003");
    //            nodeEndpoints.Add("166.62.117.163:9003");
    //            nodeEndpoints.Add("192.169.227.48:9003");
    //            nodeEndpoints.Add("162.212.156.23:9003");
    //            nodeEndpoints.Add("172.104.42.222:9003");
    //            nodeEndpoints.Add("185.12.237.78:9003");
    //            nodeEndpoints.Add("120.92.119.221:9003");
    //            nodeEndpoints.Add("120.131.7.70:9003");
    //            nodeEndpoints.Add("120.92.89.254:9003");

    //        }
    //        foreach(string endpoint in nodeEndpoints)
    //        {
    //            var ln = launchNode(coin, endpoint, n);
    //            if (ln != null)
    //            {
    //                coinNodes.Add(ln);
    //            }
    //        }
    //        return coinNodes;
    //    }

    //    /// <summary>
    //    /// Broadcast a transaction to nodes for a coin
    //    /// </summary>
    //    /// <param name="coin"></param>
    //    /// <param name="t"></param>
    //    /// <returns></returns>
    //    public static int BroadcastTransaction(string coin, Transaction transaction)
    //    {
    //        //Monitor.Enter(txlock);
    //        //try { 
                
    //            var nodes = GetNodes(coin); //Get the nodes for this coin
    //            string txhash = transaction.GetHash().ToString();
    //            int numsuccess = nodes.Count;
    //            //broadcast to all connected nodes
    //            foreach (var n in nodes)
    //            {
    //                try
    //                {
    //                    n.SendMessageAsync(new InvPayload(transaction));
    //                    n.SendMessageAsync(new TxPayload(transaction));
    //                    n.PingPong();
    //                }
    //                catch
    //                {
    //                    numsuccess -= 1;
    //                }
    //            }
    //            return numsuccess;
    //        //}
    //        //finally
    //        //{
    //        //    Monitor.Exit(txlock);
    //        //}
            
    //    }

    //    //static void AttachedNode_MessageReceived(Node node, IncomingMessage message)
    //    //{
    //    //    Debug.Write(message.Message.Command + "  : ");
    //    //    Debug.WriteLine(message.Message.Payload.ToString());

    //    //    if (message.Message.Command == "reject")
    //    //    {
    //    //        var rejectmessage = (RejectPayload)message.Message.Payload;
    //    //        response = "Reject message: " + rejectmessage.Message + " Reject reason : " + rejectmessage.Reason + " Reject code   : " + rejectmessage.Code.ToString();
    //    //        //Console.WriteLine("Reject message: " + rejectmessage.Message);
    //    //        //Console.WriteLine("Reject reason : " + rejectmessage.Reason);
    //    //        //Console.WriteLine("Reject code   : " + rejectmessage.Code.ToString());
    //    //        done = true;
    //    //    }
    //    //}
    //}

    //public class BitcoinNode
    //{
    //    string _address;
    //    int _port;
    //    static bool done = false;
    //    static string response = "";

    //    public BitcoinNode(string address, int port)
    //    {
    //        _address = address;
    //        _port = port;
    //    }

    //    public string BroadcastTransaction(Transaction t, Forks.ForkCode forkCode)
    //    {
    //        done = false;
    //        TxPayload tx = new TxPayload(t);
    //        string txhash = t.GetHash().ToString();
    //        response = txhash;
    //        var n = Network.Main;
    //        if (forkCode == Forks.ForkCode.BTF)
    //        { 
    //            n.Magic = 0xE6D4E2FA;
    //        }
    //        else if (forkCode == Forks.ForkCode.BCX)
    //        {
    //            n.Magic = 0xF9BC0511;
    //        }
    //        using (var node = Node.Connect(n, _address + ":" + _port, new NodeConnectionParameters()
    //        {
    //            Services = 0,
    //            UserAgent = "Coinpanic",
    //            IsRelay = false,
    //            Version = ProtocolVersion.SHORT_IDS_BLOCKS_VERSION_NOBAN,//forkCode == Forks.ForkCode.SBTC ? ProtocolVersion.SBTC_VERSION :
    //                      //(forkCode == Forks.ForkCode.BTF ? ProtocolVersion.BTF_VERSION :
    //                      //(forkCode == Forks.ForkCode.BCX ? ProtocolVersion.SBTC_VERSION : ProtocolVersion.WITNESS_VERSION))
    //        }))
    //        {
    //            node.MessageReceived += AttachedNode_MessageReceived;
    //            node.VersionHandshake();
    //            Debug.WriteLine("Handshake ok.");
    //            if (node.State == NodeState.Connected)
    //                Debug.WriteLine("Node is Connected.");
    //            Thread.Sleep(1000);
    //            Debug.WriteLine("Transmitting.");
    //            node.SendMessageAsync(new InvPayload(t));
    //            node.SendMessageAsync(new TxPayload(t));
    //            node.PingPong();
    //            Thread.Sleep(1000); //Give some time for a response
    //        }
            
    //        return response;
    //    }
        
    //    static void AttachedNode_MessageReceived(Node node, IncomingMessage message)
    //    {
    //        Debug.Write(message.Message.Command + "  : ");
    //        Debug.WriteLine(message.Message.Payload.ToString());

    //        if (message.Message.Command == "reject")
    //        {
    //            var rejectmessage = (RejectPayload)message.Message.Payload;
    //            response = "Reject message: " + rejectmessage.Message + " Reject reason : " + rejectmessage.Reason + " Reject code   : " + rejectmessage.Code.ToString();
    //            //Console.WriteLine("Reject message: " + rejectmessage.Message);
    //            //Console.WriteLine("Reject reason : " + rejectmessage.Reason);
    //            //Console.WriteLine("Reject code   : " + rejectmessage.Code.ToString());
    //            done = true;
    //        }

    //        //InvPayload invPayload = message.Message.Payload as InvPayload;
    //        //if (invPayload != null)
    //        //{
    //        //    done = true;
    //        //    //Console.WriteLine("Transaction accepted.");
    //        //}
    //    }
    //}

}
