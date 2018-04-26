using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json.Linq;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Configuration;
using System.Collections.Generic;

namespace NodeInterfaceLib.Tests
{
    [TestClass]
    public class TestCommands
    {
        [TestMethod]
        public void Node_BTCP_Call_GetInfo()
        {
            GetConnectionDetails("BTCP", out string host, out int port, out string user, out string pass);

            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { jsonrpc = "1.0", id = "1", method = "getinfo" });
            var response = client.Execute(request);
            string responseStr = response.Content;
            Console.WriteLine(responseStr);
        }

        [TestMethod]
        public void Node_BTCP_Call_DecodeRawTransaction()
        {
            //https://bitcoin.org/en/developer-reference#remote-procedure-calls-rpcs
            string hex = "0100000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e44937080000008a4730440220039947a62fe0fbfb7c5eba73315ddb93c55e3b938456cae8d2b4b427878bc6d00220089b4467ea4f25ea9b88d8602f08781f15b85f88aaceaff8684f794cd77702df414104ce9d21fcccea78c02182d7d6e20e87fc7f14920655ca98bc1deebb1bb0945d6a777c187e77cc5e29424a791a07494f457b0c7ebe343dd7cfc56de415a79c5ee9ffffffff011b600300000000001976a914b3b255028648e151b3e419ab6c5b2e9656ba363988ac00000000";
            GetConnectionDetails("BTCP", out string host, out int port, out string user, out string pass);
            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new { jsonrpc = "1.0", id = "1", method = "decoderawtransaction",
                @params = new List<string>() { hex },
            });

            var response = client.Execute(request);
            string responseStr = response.Content;
            Console.WriteLine(responseStr);
        }

        [TestMethod]
        public void Node_BTCP_Call_SendRawTransaction()
        {
            //https://bitcoin.org/en/developer-reference#remote-procedure-calls-rpcs
            string hex = "0100000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e44937080000008a4730440220039947a62fe0fbfb7c5eba73315ddb93c55e3b938456cae8d2b4b427878bc6d00220089b4467ea4f25ea9b88d8602f08781f15b85f88aaceaff8684f794cd77702df414104ce9d21fcccea78c02182d7d6e20e87fc7f14920655ca98bc1deebb1bb0945d6a777c187e77cc5e29424a791a07494f457b0c7ebe343dd7cfc56de415a79c5ee9ffffffff011b600300000000001976a914b3b255028648e151b3e419ab6c5b2e9656ba363988ac00000000";
            GetConnectionDetails("BTCP", out string host, out int port, out string user, out string pass);
            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "sendrawtransaction",
                @params = new List<string>() { hex },
            });

            var response = client.Execute(request);
            string responseStr = response.Content;

            //Known Errors
            if (responseStr == "{\"result\":null,\"error\":{\"code\":-25,\"message\":\"Missing inputs\"},\"id\":\"1\"}")
            {
                //Inputs already spent
            }

            Console.WriteLine(responseStr);
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
