using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin.Forks.Nodes;
using NBitcoin;
using System.Net;
using System.Threading;
using NBitcoin.Crypto;

namespace Coinpanic.Tests
{
    [TestClass]
    public class FBTCTests
    {
        [TestMethod]
        public void TestFBTCNode()
        {
            //Generate a private/public key representing this node
            Key privateKey = new Key();
            PubKey publicKey = privateKey.PubKey;

            string serverURL = "coinpanic1.eastus.cloudapp.azure.com";
            int port = 40032;
            IPAddress endpoint = null;
            endpoint = Dns.GetHostEntry(serverURL).AddressList[0];
            var ep = new IPEndPoint(endpoint.MapToIPv6Ex(), port);

            //var nodeServer = new FBTCNodeServer(Network.FBTC, NBitcoin.Protocol.ProtocolVersion.INIT_PROTO_VERSION, 1, privateKey);
            //var n = nodeServer.FindOrConnect(ep);

            //while(true)
            //{
            //    Thread.Sleep(10);
            //}
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestFBTCcrypto()
        {
            byte[] secret = new byte[2] { 0, 0 };
            var sharedsecret = Hashes.SHA512(secret);
            var AESkey = Hashes.SHA256(sharedsecret);

            int z = 1;
            Assert.Inconclusive();
        }
    }
}
