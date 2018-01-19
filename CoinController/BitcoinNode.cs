using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using SimpleTCP;
using System;
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

        BitcoinNode _node;

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
