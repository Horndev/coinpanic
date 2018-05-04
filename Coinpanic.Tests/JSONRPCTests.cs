using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using RestSharp;
using RestSharp.Authenticators;

namespace Coinpanic.Tests
{
    // Error codes: https://github.com/bitcoin/bitcoin/blob/62f2d769e45043c1f262ed45babb70fe237ad2bb/src/rpc/protocol.h#L30
    // https://bitcoin.org/en/developer-reference#remote-procedure-calls-rpcs
    [TestClass]
    public class JSONRPCTests
    {
        [TestMethod]
        public void RPC_getblockchaininfo_UBTC()
        {
            // getblockcount
            GetConnectionDetails("UBTC", out string host, out int port, out string user, out string pass);
            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "getblockchaininfo",
            });

            var restResponse = client.Execute(request);
            var content = restResponse.Content; // raw content as string
            Console.WriteLine(content);
            Assert.AreNotEqual("", content);
        }

        [TestMethod]
        public void RPC_getblockchaininfo_BTF()
        {
            // getblockcount
            GetConnectionDetails("BTF", out string host, out int port, out string user, out string pass);
            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "getblockchaininfo",
            });

            var restResponse = client.Execute(request);
            var content = restResponse.Content; // raw content as string
            Console.WriteLine(content);
            Assert.AreNotEqual("", content);
        }

        [TestMethod]
        public void RPC_getblockchaininfo_BCD()
        {
            // getblockcount
            GetConnectionDetails("BCD", out string host, out int port, out string user, out string pass);
            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "getblockchaininfo",
            });

            var restResponse = client.Execute(request);
            var content = restResponse.Content; // raw content as string
            Console.WriteLine(content);
            Assert.AreNotEqual("", content);
        }

        [TestMethod]
        public void RPC_getblockchaininfo_SBTC()
        {
            // getblockcount
            GetConnectionDetails("SBTC", out string host, out int port, out string user, out string pass);
            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "getblockchaininfo",
            });

            var restResponse = client.Execute(request);
            var content = restResponse.Content; // raw content as string
            Console.WriteLine(content);
            Assert.AreNotEqual("", content);
        }

        [TestMethod]
        public void RPC_getblockchaininfo_BTW()
        {
            // getblockcount
            GetConnectionDetails("BTW", out string host, out int port, out string user, out string pass);
            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "getblockchaininfo",
            });

            var restResponse = client.Execute(request);
            var content = restResponse.Content; // raw content as string
            Console.WriteLine(content);
            Assert.AreNotEqual("", content);
        }

        [TestMethod]
        public void RPC_getblockchaininfo_BTV()
        {
            // getblockcount
            GetConnectionDetails("BTV", out string host, out int port, out string user, out string pass);
            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "getblockchaininfo",
            });

            var restResponse = client.Execute(request);
            var content = restResponse.Content; // raw content as string
            Console.WriteLine(content);
            Assert.AreNotEqual("", content);
        }

        [TestMethod]
        public void RPC_getblockchaininfo_BPA()
        {
            // getblockcount
            GetConnectionDetails("BPA", out string host, out int port, out string user, out string pass);
            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "getblockchaininfo",
            });

            var restResponse = client.Execute(request);
            var content = restResponse.Content; // raw content as string
            Console.WriteLine(content);
            Assert.AreNotEqual("", content);
        }

        private static void GetConnectionDetails(string coin, out string host, out int port, out string user, out string pass)
        {
            host = GetHost(coin);
            port = GetPort(coin);
            user = GetUser(coin);
            pass = GetPass(coin);
        }

        static string GetHost(string coin)
        {
            return ConfigurationManager.AppSettings[coin + "Host"];
        }

        static int GetPort(string coin)
        {
            return Convert.ToInt32(ConfigurationManager.AppSettings[coin + "Port"]);
        }

        static string GetUser(string coin)
        {
            return ConfigurationManager.AppSettings[coin + "User"];
        }

        static string GetPass(string coin)
        {
            return ConfigurationManager.AppSettings[coin + "Pass"];
        }
    }
}
