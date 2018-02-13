using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Forks;
using NBitcoin.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoinpanicLib.NodeConnection
{
    public class TxDetails
    {
        public string Result;
        public string Coin;
        public bool IsError;
        public bool IsMined;
        public bool IsInput;
        public int NumBroadcasts;
        public Transaction transaction;
        public Node node;
    }

    public class NodeDetails
    {
        public string coin;                 //which coin
        public string ip;                  // Where to connect:  "x.x.x.x:ppp"
        public int port;
        public int numDisconnects;         // Number of disconnects
        public DateTime lastDisconnect;    // When last disconnected (to not spam connect)
        public bool use;                   // If false, will not use
    }

    /// <summary>
    /// This is a singleton class for the web service
    /// </summary>
    public class CoinPanicNodeService : INodeService
    {
        //Our NodeServer
        private NodeServer nodeServer = null;

        private static ConcurrentDictionary<string, TxDetails> txSent = new ConcurrentDictionary<string, TxDetails>();
        private static ConcurrentDictionary<string, TxDetails> txRecv = new ConcurrentDictionary<string, TxDetails>();
        private static ConcurrentDictionary<string, IPAddress> epCache = new ConcurrentDictionary<string, IPAddress>();

        public static ConcurrentQueue<IPEndPoint> advertisedPeers = new ConcurrentQueue<IPEndPoint>();

        private static string externalip = new WebClient().DownloadString("http://icanhazip.com").Trim('\n');

        private static RejectPayload lastReject = null;
        private static DateTime lastRejectTime = DateTime.Now;

        private ConcurrentDictionary<IPEndPoint, DateTime> LastConnectAttempt = new ConcurrentDictionary<IPEndPoint, DateTime>();

        private string coin;
        private string emailhost;
        private int emailport;
        private string emailuser;
        private string emailpass;

        public string Coin { get => coin; set => coin = value; }

        public int NumConnectedPeers
        {
            get
            {
                
                if (nodeServer == null)
                    return 0;
                Debug.WriteLine("NumConnectedPeers: " + nodeServer.ConnectedNodes.Count);
                return nodeServer.ConnectedNodes.Count;
            }
        }

        public CoinPanicNodeService()
        {
            // Initialize to server the coin in configuration
            coin = ConfigurationManager.AppSettings["ServiceCoin"];
            emailhost = System.Configuration.ConfigurationManager.AppSettings["EmailSMTPHost"];
            emailport = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["EmailSMTPPort"]);
            emailuser = System.Configuration.ConfigurationManager.AppSettings["EmailUser"];
            emailpass = System.Configuration.ConfigurationManager.AppSettings["EmailPass"];

            Debug.Print("Launching Node Service");

            NodeConnectionParameters p = new NodeConnectionParameters()
            {
                Services = NodeServices.Nothing,            // Yeah, I'm a leech
                UserAgent = "coinpanic.com",                // The definition of middleman
                IsRelay = false,                            // Just, don't talk to me
                Version = BitcoinForks.ForkByShortName[coin].ProtocolVersion,//coin == "BCD" ? ProtocolVersion.SHORT_IDS_BLOCKS_VERSION_NOBAN : ProtocolVersion.FORK_VERSION,     // I hack it
            };

            Network n = BitcoinForks.ForkByShortName[coin].Network;
            
            nodeServer = new NodeServer(n, BitcoinForks.ForkByShortName[coin].ProtocolVersion);
            nodeServer.NodeRemoved += Svr_NodeRemoved;
            nodeServer.MessageReceived += Svr_MessageReceived;
            nodeServer.InboundNodeConnectionParameters = p;
            nodeServer.AllowLocalPeers = true;
            nodeServer.ExternalEndpoint = new IPEndPoint(IPAddress.Parse(externalip).MapToIPv6Ex(), n.DefaultPort);
            nodeServer.LocalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6Ex(), n.DefaultPort);
            //nodeServer.Listen();
            Debug.WriteLine("Node Service Listening");
        }

        public TxDetails BroadcastTransaction(Transaction t, bool force = false)
        {
            string txhash = t.GetHash().ToString();
            Debug.WriteLine("BroadcastTransaction: " + txhash);
            var connectedNodes = nodeServer.ConnectedNodes.Where(n => n.State == NodeState.HandShaked);
            bool donebroadcasting = false;

            //Prepare the result
            TxDetails result = new TxDetails
            {
                Coin = coin,
                IsError = false,
                IsMined = false,
                IsInput = false,
                Result = "Not Broadcast",
                transaction = t,
                NumBroadcasts = 0,
            };

            //broadcast until sent.
            foreach (var n in connectedNodes)
            {
                if (donebroadcasting)
                {
                    Debug.WriteLine("donebroadcasting: " + txhash);
                    //txSent.TryRemove(txhash, out result); //Remove if it was there (clear the queue)
                    result = txRecv.TryGet(txhash);
                    break;
                }
                try
                {
                    var sw = Stopwatch.StartNew();
                    sw.Start();
                    //if the has is already sent, don't send again.
                    if (!txSent.ContainsKey(txhash))
                    {
                        Debug.WriteLine("Broadcasting: " + txhash);
                        if (!txSent.TryAdd(txhash, result))
                        {
                            //Error adding to mempool
                            Debug.Write("Error adding to mempool: " + txhash);
                            result.Result = "Error inserting transaction into mempool.";
                            result.IsError = true;
                        }
                        //n.SendMessageAsync(new InvPayload(t));  // Let the node know it is in our mempool
                        n.SendMessageAsync(new TxPayload(t));   // Broadcast the tx to the node
                        n.PingPong();
                        result.Result = "Broadcast";
                        result.NumBroadcasts += 1;
                        
                    }

                    //The transaction has been sent - wait up to 10 seconds until not in
                    while (txSent.ContainsKey(txhash) && sw.ElapsedMilliseconds < 10000)
                    {
                        //Still hasn't been confirmed
                        Thread.Sleep(100);  //wait for it.
                    }

                    //Did it get picked up?
                    TxDetails response = null;
                    if (!txRecv.ContainsKey(txhash))
                    {
                        Debug.WriteLine("Still not confirmed: " + txhash);
                        //no
                        result.Result = "Broadcast, no errors returned, not confirmed.";
                        // Can't declare it an error
                        SendMail(result.Coin + " transaction no response " + t.TotalOut.ToString(), result.Result);
                        //timed out - ask again if it was received (give 5 seconds)
                        n.SendMessageAsync(new GetDataPayload(new InventoryVector(InventoryType.MSG_TX, t.GetHash())));
                        sw.Restart();
                        
                        while (sw.ElapsedMilliseconds < 5000 && !txRecv.TryGetValue(txhash, out response))
                        {
                            Thread.Sleep(100);  //wait for it.
                        }
                        if (response != null)
                        {
                            SendMail(result.Coin + " transaction confirmed " + t.TotalOut.ToString(), result.Result);
                            donebroadcasting = true;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Confirmed: " + txhash);
                        if (txRecv.ContainsKey(txhash))
                        {
                            SendMail(result.Coin + " transaction confirmed " + t.TotalOut.ToString(), result.Result);
                            donebroadcasting = true;
                        }
                    }
                }
                catch
                {
                    txSent.TryRemove(txhash, out result); //Remove if it was there
                    Debug.WriteLine("Broadcast failed: " + txhash);
                    result.IsError = true;
                    //we'll loop around and try another node.
                }
            }
            return result;
        }

        public NodesStatus GetConnectedPeers
        {
            get
            {
                List<NodeStatus> nodestatus = new List<NodeStatus>();

                foreach (var n in nodeServer.ConnectedNodes)
                {
                    nodestatus.Add(new NodeStatus()
                    {

                        IP = (n.State == NBitcoin.Protocol.NodeState.HandShaked || n.State == NBitcoin.Protocol.NodeState.Connected) ? n.Peer.Endpoint.Address.ToString() : "",
                        name = (n.State == NBitcoin.Protocol.NodeState.HandShaked || n.State == NBitcoin.Protocol.NodeState.Connected) ? n.PeerVersion.UserAgent : "",
                        port = (n.State == NBitcoin.Protocol.NodeState.HandShaked || n.State == NBitcoin.Protocol.NodeState.Connected) ? Convert.ToString(n.Peer.Endpoint.Port) : "",
                        Status = n.State.ToString(),
                        uptime = (n.State == NBitcoin.Protocol.NodeState.HandShaked || n.State == NBitcoin.Protocol.NodeState.Connected) ? n.Peer.Ago.ToString() : "",//n.Counter.Start.ToUniversalTime().ToShortDateString() + " " +n.Counter.Start.ToUniversalTime().ToLongTimeString(): "",
                        version = n.State == NBitcoin.Protocol.NodeState.HandShaked ? Convert.ToString(n.PeerVersion.Version) : "",
                    });
                }
                NodesStatus res = new NodesStatus() { Nodes = nodestatus };
                return res;
            }
        }

        public Node TryGetNode(string ip, int port)
        {
            try
            {
                IPAddress endpoint = null;
                if (!epCache.TryGetValue(ip, out endpoint))
                {
                    endpoint = Dns.GetHostEntry(ip).AddressList[0];
                    epCache.TryAdd(ip, endpoint);
                }
                var ep = new IPEndPoint(endpoint.MapToIPv6Ex(), port);
                var node = nodeServer.ConnectedNodes.FindByEndpoint(ep);
                return node;
            }
            catch
            {
                return null;
            }
        }

        public void ConnectNode(NodeDetails sn, bool force = false)
        {
            if (sn.use || force)
            {
                try
                {
                    IPAddress endpoint = null;
                    if (!epCache.TryGetValue(sn.ip, out endpoint))
                    {
                        if (!IPAddress.TryParse(sn.ip, out IPAddress addr))
                        {
                            //try to resolve a host name
                            try
                            {
                                endpoint = Dns.GetHostEntry(sn.ip).AddressList[0];
                            }
                            catch (Exception e)
                            {
                                Debug.Write(e.Message);
                            }
                        }
                        else
                        {
                            endpoint = addr;
                        }
                        epCache.TryAdd(sn.ip, endpoint);
                    }

                    var ep = new IPEndPoint(endpoint.MapToIPv6Ex(), sn.port);
                    if (!force && LastConnectAttempt.TryGetValue(ep, out DateTime lasttime))
                    {
                        if (DateTime.Now - lasttime < TimeSpan.FromMinutes(5))
                        {
                            Debug.Write("Don't hammer by trying to connect too often.");
                            return;
                        }
                    }
                    else
                    {
                        LastConnectAttempt.AddOrUpdate(ep, DateTime.Now, (key, oldval) => DateTime.Now);
                    }
                    Debug.WriteLine("Connecting (" + coin + ") to " + endpoint + ":" + Convert.ToString(sn.port));
                    LastConnectAttempt[ep] = DateTime.Now;
                    var node = nodeServer.FindOrConnect(ep);
                    Thread.Sleep(0); //Don't underestimate thread preemption... 
                    Thread.Sleep(100);
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
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Failed Connecting (" + coin + ") to " + sn.ip + ":" + Convert.ToString(sn.port) + " " + e.Message);
                }
            }
        }

        public void DisconnectNode(NodeDetails n)
        {
            Node mynode = TryGetNode(n.ip, n.port);
            mynode.Disconnect("Disconnect requested");
        }

        public void ConnectNodes(List<NodeDetails> seedNodes, int maxnodes = 3)
        {
            if (seedNodes == null)
                throw new ArgumentException("No seed nodes provided to Initialize nodes.");

            var coins = seedNodes.Select(n => n.coin)
                .Distinct().ToList();

            foreach (string coin in coins)
            {
                int numconnected = 0;
                foreach (var sn in seedNodes.Where(c => c.coin == coin))
                {
                    //coinNodeDetail = new BlockingCollection<NodeDetails>();
                    if (sn.use && numconnected <= maxnodes)
                    {
                        try
                        {
                            IPAddress endpoint = null;
                            if (!epCache.TryGetValue(sn.ip, out endpoint))
                            {
                                if (!IPAddress.TryParse(sn.ip, out IPAddress addr))
                                {
                                    //try to resolve a host name
                                    try
                                    {
                                        endpoint = Dns.GetHostEntry(sn.ip).AddressList[0];
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.Write(e.Message);
                                        continue;
                                    }
                                }
                                else
                                {
                                    endpoint = addr;
                                }
                                epCache.TryAdd(sn.ip, endpoint);
                            }
                            
                            var ep = new IPEndPoint(endpoint.MapToIPv6Ex(), sn.port);
                            if (LastConnectAttempt.TryGetValue(ep, out DateTime lasttime))
                            {
                                if (DateTime.Now - lasttime < TimeSpan.FromMinutes(5))
                                {
                                    Debug.Write("Don't hammer by trying to connect too often.");
                                    continue;
                                }
                            }
                            else
                            {
                                LastConnectAttempt.AddOrUpdate(ep, DateTime.Now, (key, oldval) => DateTime.Now );
                            }
                            Debug.WriteLine("Connecting (" + coin + ") to " + endpoint + ":" + Convert.ToString(sn.port));
                            LastConnectAttempt[ep] = DateTime.Now;
                            
                            var node = nodeServer.FindOrConnect(ep);
                            Thread.Sleep(0); //Don't underestimate thread preemption... 
                            Thread.Sleep(100);
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
                        }
                    }
                }
            }
        }

        private void SendMail(string subject, string messagetx)
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

        #region MessageHandlers

        /*Broadcasting: 7ddf70a172fcb15a4960ba295ddb6dce5781457fd7ec70cce6e8d447f34acb24
::ffff:52.179.80.73:getdata  : GetDataPayload
::ffff:52.179.80.73:getdata  : GetDataPayload
::ffff:52.179.80.73:pong  : PongPayload : 6439942432647925556
Still not confirmed: 7ddf70a172fcb15a4960ba295ddb6dce5781457fd7ec70cce6e8d447f34acb24
::ffff:52.179.80.73:notfound  : Count: 1*/

        /// <summary>
        /// Event handler for dropped nodes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="node"></param>
        private static void Svr_NodeRemoved(NodeServer sender, Node node)
        {
            Debug.WriteLine("disconnect " + node.RemoteSocketEndpoint.Address.ToString());

            //initialize reconnect?
        }

        private static void Svr_MessageReceived(NodeServer sender, IncomingMessage message)
        {
            Debug.Write(message.Node.Peer.Endpoint.Address.ToString() + ":" + message.Message.Command + "  : ");
            Debug.WriteLine(message.Message.Payload.ToString());

            if (message.Message.Command == "notfound")
            {
                var data = (NotFoundPayload)message.Message.Payload;
                foreach (var i in data)
                {
                    Debug.WriteLine(i.Type.ToString());
                    Debug.WriteLine(i.Hash.ToString());
                    //    //if (i.Hash == txhash) //received message

                    if (i.Type == InventoryType.MSG_TX)
                    {
                        //Received a transaction
                        if (txSent.ContainsKey(i.Hash.ToString()))
                        {
                            if (txSent.TryRemove(i.Hash.ToString(), out TxDetails txInfo))
                            {
                                if (DateTime.Now - lastRejectTime < TimeSpan.FromSeconds(20))
                                {
                                    txInfo.Result = "Rejected: " + lastReject.Message + ", Reason: " + lastReject.Reason + ", Code: " + lastReject.Code.ToString();
                                    txInfo.IsError = true;
                                    //add it to the list of completed transactions
                                    txRecv.TryAdd(i.Hash.ToString(), txInfo);
                                }
                                else
                                {
                                    txInfo.Result = "Transaction was rejected.  This is often caused when the coins are already spent, or if the transaction inputs were not found in the blockchain.";
                                    txInfo.IsError = true;

                                    //add it to the list of completed transactions
                                    txRecv.TryAdd(i.Hash.ToString(), txInfo);
                                }
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

                //var subject = "transaction rejected";
                //var messagetx = "Reject message: " + rejectmessage.Message + "Reject reason : " + rejectmessage.Reason + "Reject code   : " + rejectmessage.Code.ToString();
                lastReject = rejectmessage;
                lastRejectTime = DateTime.Now;
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
                //else
                //{
                //    Debug.WriteLine("Rejected tx not found in mempool.");
                //}
                //if (txRecv.ContainsKey(rejectmessage.Hash.ToString()))
                //{
                //    Debug.WriteLine("We already got a response for " + rejectmessage.Hash.ToString());
                //}

                //SendMail(subject, messagetx);
            }
            else if (message.Message.Command == "addr")
            {
                var addrmessage = (AddrPayload)message.Message.Payload;
                Debug.WriteLine("Recieved addresses: ");
                foreach (var a in addrmessage.Addresses)
                {
                    advertisedPeers.Enqueue(a.Endpoint);
                    Debug.WriteLine(a.Endpoint.Address.ToString());
                }
            }
            else if (message.Message.Command == "tx")
            {
                var txmessage = message.Message.Payload as TxPayload;
                if (txmessage != null)
                {
                    Debug.WriteLine("Recieved tx: ");
                    var tx = txmessage.Object;
                    if (tx != null)
                    {
                        var h = tx.GetHash();
                        //Received a transaction
                        if (txSent.ContainsKey(h.ToString()))
                        {
                            //We sent this
                            if (txSent.TryRemove(h.ToString(), out TxDetails txInfo))
                            {
                                txInfo.Result = "Broadcast successful.  Transaction inserted into mempool.";
                                txInfo.IsError = false;

                                //add it to the list of completed transactions
                                txRecv.TryAdd(h.ToString(), txInfo);
                            }
                        }
                        if (txRecv.ContainsKey(h.ToString()))
                        {
                            Debug.WriteLine("We already got a response for " + h.ToString());
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Error decoding tx in message.");
                    }
                }
                else
                {
                    Debug.WriteLine("Error decoding tx message.");
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
                                txInfo.node.SendMessageAsync(new TxPayload(txInfo.transaction));
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

        #endregion

        private string val;

        public string Val { get => val; set => val = value; }


        public void Test()
        {
            Debug.WriteLine("CPS Test: " + val);
        }

        
    }
}
