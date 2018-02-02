using NBitcoin;
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

        private ConcurrentDictionary<string, TxDetails> txSent = new ConcurrentDictionary<string, TxDetails>();
        private ConcurrentDictionary<string, TxDetails> txRecv = new ConcurrentDictionary<string, TxDetails>();

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

            string externalip = new WebClient().DownloadString("http://icanhazip.com").Trim('\n');

            NodeConnectionParameters p = new NodeConnectionParameters()
            {
                Services = NodeServices.Nothing,            // Yeah, I'm a leech
                UserAgent = "coinpanic.com",                // The definition of middleman
                IsRelay = false,                            // Just, don't talk to me
                Version = BitcoinForks.ForkByShortName[coin].ProtocolVersion,//coin == "BCD" ? ProtocolVersion.SHORT_IDS_BLOCKS_VERSION_NOBAN : ProtocolVersion.FORK_VERSION,     // I hack it
            };

            Network n = BitcoinForks.ForkByShortName[coin].Network;

            nodeServer = new NodeServer(n);
            nodeServer.NodeRemoved += Svr_NodeRemoved;
            nodeServer.MessageReceived += Svr_MessageReceived;
            nodeServer.InboundNodeConnectionParameters = p;
            nodeServer.AllowLocalPeers = true;
            nodeServer.ExternalEndpoint = new IPEndPoint(IPAddress.Parse(externalip).MapToIPv6Ex(), n.DefaultPort);
            nodeServer.LocalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6Ex(), n.DefaultPort);
            nodeServer.Listen();
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
                    txSent.TryRemove(txhash, out result); //Remove if it was there (clear the queue)
                    result = txRecv.TryGet(txhash);
                    break;
                }
                try
                {
                    var sw = Stopwatch.StartNew();
                    sw.Start();
                    //if the has is already sent, don't send again.
                    if (!txSent.TryAdd(txhash, result))
                    {
                        Debug.WriteLine("Broadcasting: " + txhash);
                        n.SendMessageAsync(new InvPayload(t));
                        n.SendMessageAsync(new TxPayload(t));
                        n.PingPong();
                    }

                    result.Result = "Broadcast";
                    result.NumBroadcasts += 1;

                    //The transaction has been sent - wait up to 10 seconds until not in
                    while (txSent.ContainsKey(txhash) && sw.ElapsedMilliseconds < 10000)
                    {
                        //Still hasn't been confirmed
                        Thread.Sleep(100);  //wait for it.
                    }

                    //Did it get picked up?
                    TxDetails response = null;
                    if (txSent.ContainsKey(txhash))
                    {
                        Debug.WriteLine("Still not confirmed: " + txhash);
                        //no
                        result.Result = "Broadcast, no errors returned, not confirmed.";
                        // Can't declare it an error

                        //timed out - ask again if it was received (give 5 seconds)
                        n.SendMessageAsync(new GetDataPayload(new InventoryVector(InventoryType.MSG_TX, t.GetHash())));
                        sw.Restart();
                        
                        while (sw.ElapsedMilliseconds < 5000 && !txRecv.TryGetValue(txhash, out response))
                        {
                            Thread.Sleep(100);  //wait for it.
                        }
                        if (response != null)
                        {
                            donebroadcasting = true;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Confirmed: " + txhash);
                        if (txRecv.ContainsKey(txhash))
                        {
                            donebroadcasting = true;
                        }
                    }
                }
                catch
                {
                    txSent.TryRemove(txhash, out result); //Remove if it was there
                    Debug.WriteLine("Broadcast failed: " + txhash);
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
                            var endpoint = Dns.GetHostEntry(sn.ip).AddressList[0];
                            Debug.WriteLine("Connecting (" + coin + ") to " + endpoint + ":" + Convert.ToString(sn.port));
                            var node = nodeServer.FindOrConnect(new IPEndPoint(endpoint.MapToIPv6Ex(), sn.port));
                            Thread.Sleep(0); //Don't underestimate thread preemption... 
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
                //var data = (NotFoundPayload)message.Message.Payload;
                //foreach (var i in data)
                //{
                //    //if (i.Hash == txhash) //received message
                //    Debug.WriteLine(i.Type.ToString());
                //    Debug.WriteLine(i.Hash.ToString());
                //    if (i.Type == InventoryType.MSG_TX)
                //    {
                //        //Received a transaction
                //        if (txSent.ContainsKey(i.Hash.ToString()))
                //        {
                //            if (txSent.TryRemove(i.Hash.ToString(), out TxDetails txInfo))
                //            {
                //                txInfo.Result = "Transaction was broadcast to the network, but confirmation was not received.  Check your balance.";
                //                txInfo.IsError = true;

                //                //add it to the list of completed transactions
                //                txRecv.TryAdd(i.Hash.ToString(), txInfo);
                //            }
                //        }
                //    }
                //}
            }
            else if (message.Message.Command == "reject")
            {
                var rejectmessage = (RejectPayload)message.Message.Payload;
                Debug.WriteLine("Reject message: " + rejectmessage.Message);
                Debug.WriteLine("Reject reason : " + rejectmessage.Reason);
                Debug.WriteLine("Reject code   : " + rejectmessage.Code.ToString());

                //var subject = "transaction rejected";
                //var messagetx = "Reject message: " + rejectmessage.Message + "Reject reason : " + rejectmessage.Reason + "Reject code   : " + rejectmessage.Code.ToString();

                //if (txSent.ContainsKey(rejectmessage.Hash.ToString()))
                //{
                //    //We sent this
                //    if (txSent.TryRemove(rejectmessage.Hash.ToString(), out TxDetails txInfo))
                //    {
                //        txInfo.Result = "Rejected: " + rejectmessage.Message + ", " + rejectmessage.Reason + " Code " + rejectmessage.Code.ToString();
                //        txInfo.IsError = true;
                //        //add it to the list of completed transactions
                //        txRecv.TryAdd(rejectmessage.Hash.ToString(), txInfo);
                //    }
                //}
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
                    Debug.WriteLine(a.Endpoint.Address.ToString());
                }
            }
            else if (message.Message.Command == "feefilter")
            {

            }
            else if (message.Message.Command == "inv")
            {
                //// Sent when 
                //var data = (InvPayload)message.Message.Payload;
                //foreach (var i in data)
                //{
                //    //if (i.Hash == txhash) //received message
                //    Debug.WriteLine(i.Type.ToString());
                //    Debug.WriteLine(i.Hash.ToString());
                //    if (i.Type == InventoryType.MSG_TX)
                //    {
                //        //Received a transaction
                //        if (txSent.ContainsKey(i.Hash.ToString()))
                //        {
                //            //We sent this
                //            if (txSent.TryRemove(i.Hash.ToString(), out TxDetails txInfo))
                //            {
                //                txInfo.Result = "Success";
                //                txInfo.IsError = false;

                //                //add it to the list of completed transactions
                //                txRecv.TryAdd(i.Hash.ToString(), txInfo);
                //            }
                //        }
                //        if (txRecv.ContainsKey(i.Hash.ToString()))
                //        {
                //            Debug.WriteLine("We already got a response for " + i.Hash.ToString());
                //        }
                //    }
                //}
            }
            else if (message.Message.Command == "sendcmpct")
            {

            }
            else if (message.Message.Command == "getdata")
            {
                //var h = new HexEncoder();
                //var data = (GetDataPayload)message.Message.Payload;
                //foreach (var i in data.Inventory)
                //{
                //    Debug.WriteLine(i.Type.ToString());
                //    Debug.WriteLine(i.Hash.ToString());
                //    if (i.Type == InventoryType.MSG_TX)
                //    {
                //        //Received a transaction
                //        if (txSent.ContainsKey(i.Hash.ToString()))
                //        {
                //            //They are asking to see our transaction.  Show them our papers.
                //            if (txSent.TryGetValue(i.Hash.ToString(), out TxDetails txInfo))
                //            {
                //                Debug.WriteLine("Rebroadcast transaction by request.");
                //                txInfo.n.SendMessageAsync(new TxPayload(txInfo.tx));
                //            }

                //        }
                //        else
                //        {
                //            Debug.WriteLine("Sending notfound");
                //            message.Node.SendMessageAsync(new NotFoundPayload(i));
                //        }
                //    }
                //}

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
