using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using NBitcoin.DataEncoders;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Collections.Generic;
using LightningLib.lndrpc;
using System.Threading;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using QRCoder;
using System.Drawing;
using System.Configuration;

namespace LightningNetworkTests
{
    [TestClass]
    public class lndTests
    {
        // These are sensitive: load from config
        static Dictionary<bool, string> MacaroonAdmin = new Dictionary<bool, string>()
        {
            {true, ConfigurationManager.AppSettings["LnTestnetMacaroonAdmin"] },
            {false, ConfigurationManager.AppSettings["LnMainnetMacaroonAdmin"] },
        };

        static Dictionary<bool, string> MacaroonInvoice = new Dictionary<bool, string>()
        {
            {true, ConfigurationManager.AppSettings["LnTestnetMacaroonInvoice"] },
            {false, ConfigurationManager.AppSettings["LnTestnetMacaroonInvoice"] },
        };

        // true=testnet
        static Dictionary<bool, string> MacaroonRead = new Dictionary<bool, string>()
        {
            {true, ConfigurationManager.AppSettings["LnTestnetMacaroonRead"] },
            {false, ConfigurationManager.AppSettings["LnMainnetMacaroonRead"] },
        };

        [TestMethod]
        public void VerifyAppDomainHasConfigurationSettings()
        {
            string value = ConfigurationManager.AppSettings["UseTestNet"];
            Assert.IsFalse(String.IsNullOrEmpty(value), "No App.Config found.");
        }

        [TestMethod]
        public void TestLndQR()
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode("The text which should be encoded.", QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            Assert.IsNotNull(qrCodeImage);
        }

        [TestMethod]
        public void Test_CallGetnodeinfoAsStringMainnet()
        {
            string host = ConfigurationManager.AppSettings["LnMainnetHost"];
            string restpath = "/v1/graph/node/{pub_key}";

            //This may not exist
            string pubkey = "032925faba461d86f51c2e019fce9f8929795bf974b5114a73b5c8ad263d6a2c5e";

            string response = LndApiGet(
                host: host,
                restpath: restpath,
                urlParameters: new Dictionary<string, string>() {{"pub_key", pubkey}},
                macaroonRead: MacaroonRead[false]);

            Console.WriteLine(response);
        }

        [TestMethod]
        public void TestAPI_Mainnet_Node_CallGetnodeinfo_AsString()
        {
            string host = ConfigurationManager.AppSettings["LnMainnetHost"];

            string pubkey = "032925faba461d86f51c2e019fce9f8929795bf974b5114a73b5c8ad263d6a2c5e";

            var client = new LndRpcClient(host, macaroonRead: MacaroonRead[false]);
            var ni = client.GetNodeInfo(pubkey);

            Console.WriteLine(ni.node.alias);
        }

        [TestMethod]
        public void TestAPI_Mainnet_Invoice_CreateInvoice()
        {
            //request a new invoice
            string host = ConfigurationManager.AppSettings["LnMainnetHost"];
            string restpath = "/v1/invoices";
            var invoice = new Invoice()
            {
                value = "1000",
                memo = "Testing",
                expiry = "432000",
            };

            //admin

            string responseStr = LndRpcClient.LndApiPostStr(host, restpath, invoice, adminMacaroon: MacaroonAdmin[false]);
            Console.WriteLine(responseStr);
        }

        [TestMethod]
        public void TestAPI_Mainnet_Invoice_DecodeInvoice_AsString()
        {
            string host = ConfigurationManager.AppSettings["LnMainnetHost"];
            string restpath = "/v1/payreq/{pay_req}";

            // This one includes a memo
            string payreq = "lnbc90n1pdd4xzqpp5j69daa4ep5nfzgx0pdfajcfn96ysjts7ekdhlcw4kt6g8w2ff79qdqc235xjueqd9ejqmteypkk2mt0cqzys2vk4ecwl0lf0dhwplrphvznpmkw6ehv6p3w5rtfux7u9963azu0hmg3fhn4w85qugxapecqf7dmehajxtk9c5zvxw22l77vr2j645qcqs8yrq6";

            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                {"pay_req",  payreq},
            };

            string responseStr = LndRpcClient.LndApiGetStr(host, restpath, urlParameters: parameters, adminMacaroon: MacaroonRead[false]);
            Console.WriteLine(responseStr);
            string expected = "{\"destination\":\"03a9d79bcfab7feb0f24c3cd61a57f0f00de2225b6d31bce0bc4564efa3b1b5aaf\",\"payment_hash\":\"968adef6b90d269120cf0b53d961332e89092e1ecd9b7fe1d5b2f483b9494f8a\",\"num_satoshis\":\"9\",\"timestamp\":\"1524275264\",\"expiry\":\"3600\",\"description\":\"This is my memo\",\"cltv_expiry\":\"144\"}";
            Assert.AreEqual(expected, responseStr);
        }

        #region testnet

        [TestMethod]
        public void TestAPIDecodeInvoiceTestnetAsString()
        {
            string host = ConfigurationManager.AppSettings["LnTestnetHost"];
            string restpath = "/v1/payreq/{pay_req}";

            string payreq = "lntb4m1pdv9jf4pp5dnk8sq4d0hg0rwwge4l09zjg7mwkz2kdy8z6qynq332l300405rsdqqcqzysz376stul9zuxersermhtedgga3dzq0pmzh7zddz3wvd0kuzsldmzl4aefcn6ph8d4hlfxlesgn9h4j0m2zl2kajc2kn4yv3nyv96n0cpfv2kuw";
            //"lnbc90n1pdd4xzqpp5j69daa4ep5nfzgx0pdfajcfn96ysjts7ekdhlcw4kt6g8w2ff79qdqc235xjueqd9ejqmteypkk2mt0cqzys2vk4ecwl0lf0dhwplrphvznpmkw6ehv6p3w5rtfux7u9963azu0hmg3fhn4w85qugxapecqf7dmehajxtk9c5zvxw22l77vr2j645qcqs8yrq6";
            //

            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                {"pay_req",  payreq},
            };

            string responseStr = LndRpcClient.LndApiGetStr(host, restpath, urlParameters:parameters, adminMacaroon: MacaroonRead[true]);
            Console.WriteLine(responseStr);
        }

        [TestMethod]
        public void TestAPIPayInvoiceTestnetAsString()
        {
            string host = ConfigurationManager.AppSettings["LnTestnetHost"];
            string payreq = "lntb50n1pdv9nlppp5unpfcqu88d07g7raaqs9dgpc47vznsz63pa8l4nmf5yuancxutmqdqqcqzysgrkr424w8s3kgpvn3xzcg7xth3ax4uuu5vduha3z5ullv522e56qetr6hmlvqmdydxdawrszwa52hfntmajghf7u5nppvw93keqhsecpqsydu6";

            //Need to first decode the payment request
            var client = new LndRpcClient(host, macaroonAdmin: MacaroonAdmin[true], macaroonRead: MacaroonRead[true]);
            var payment = client.DecodePayment(payreq);

            string restpath = "/v1/channels/transactions";

            var payreqParam = new { payment_request = payreq };

            string responseStr = LndRpcClient.LndApiPostStr(host, restpath, payreqParam, adminMacaroon: MacaroonAdmin[true]);
            Console.WriteLine(responseStr);
        }

        [TestMethod]
        public void TestLndRpcClientTestnet()
        {
            bool useTestnet = true;
            var LndClient = new LndRpcClient(
                macaroonAdmin: MacaroonAdmin[useTestnet]);

        }

        #endregion

        [TestMethod]
        public void TestAPIGetInfo()
        {
            string macaroon = ConfigurationManager.AppSettings["LnMainnetMacaroonRead"];

            string host = ConfigurationManager.AppSettings["LnMainnetHost"];
            string restpath = "/v1/getinfo";
            GetInfoResponse info = LndApiGetObj<GetInfoResponse>(host, restpath, mac: macaroon);
            Console.WriteLine(info.alias);
        }

        private static T LndApiGetObj<T>(string host, string restpath, int port = 8080, string mac = "") where T: new()
        {
            string macaroon = mac;
            if (mac == "")
            {
                var m = System.IO.File.ReadAllBytes("readonly.macaroon");
                HexEncoder h = new HexEncoder();
                macaroon = h.EncodeData(m);
            }
            //string TLSFilename = "tls.cert";
            //X509Certificate2 certificates = new X509Certificate2();
            //certificates.Import(System.IO.File.ReadAllBytes(TLSFilename));

            var client = new RestClient("https://" + host + ":" + Convert.ToString(port));
            //client.ClientCertificates = new X509CertificateCollection() { certificates };

            //X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            //store.Open(OpenFlags.ReadWrite);
            //store.Add(certificates);

            client.RemoteCertificateValidationCallback =
                delegate (object s, X509Certificate certificate,
                          X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    //TODO: fix later
                    return true;
                };

            var request = new RestRequest(restpath, Method.GET);
            request.AddHeader("Grpc-Metadata-macaroon", macaroon);

            IRestResponse<T> response = client.Execute<T>(request);
            T info = response.Data;
            return info;
        }

        [TestMethod]
        public void TestAPICallSwitchAsString()
        {
            string host = ConfigurationManager.AppSettings["LnMainnetHost"];
            string restpath = "/v1/switch";
            var reqObj = new FwdRequest()
            {
                start_time = "0",
                end_time = "999999999999",
                index_offset = 0,
                num_max_events = 1000,
            };

            string responseStr = LndRpcClient.LndApiPostStr(host, restpath, reqObj, 
                adminMacaroon: ConfigurationManager.AppSettings["LnMainnetMacaroonRead"]);
            Console.WriteLine(responseStr);
        }

        [TestMethod]
        public void TestAPIGetInvoiceAsString()
        {
            string host = ConfigurationManager.AppSettings["LnMainnetHost"];
            string restpath = "/v1/invoice";
            var reqObj = new FwdRequest()
            {
                start_time = "0",
                end_time = "999999999999",
                index_offset = 0,
                num_max_events = 1000,
            };

            string responseStr = LndRpcClient.LndApiPostStr(host, restpath, reqObj, 
                adminMacaroon: ConfigurationManager.AppSettings["LnMainnetMacaroonRead"]);
            Console.WriteLine(responseStr);
        }

        [TestMethod]
        public void TestAPISubscribeInvoice()
        {
            string host = ConfigurationManager.AppSettings["LnMainnetHost"];
            string restpath = "/v1/invoices/subscribe";
            LndApiGetStrAsync(host, restpath, adminMacaroon: MacaroonAdmin[false]);

            for(int i = 0; i < 5; i++)
            {
                Console.WriteLine(i.ToString());
                Thread.Sleep(3000);
                if (i < 2)
                {
                    restpath = "/v1/invoices";
                    var invoice = new Invoice()
                    {
                        value = "1000",
                        memo = "Testing",
                    };

                    //admin
                    string responseStr = LndRpcClient.LndApiPostStr(host, restpath, invoice, adminMacaroon: MacaroonAdmin[false]);
                    Console.WriteLine("added: " + responseStr);
                }
            }
            Console.WriteLine("Done");
        }

        public static void LndApiGetStrAsync(string host, string restpath, int port = 8080, string adminMacaroon = "")
        {
            //var m = System.IO.File.ReadAllBytes("readonly.macaroon");
            HexEncoder h = new HexEncoder();
            string macaroon = "";
            if (adminMacaroon != "")
            {
                macaroon = adminMacaroon;
            }
            else
            {
                throw new Exception("No admin macaroon provided.");
            }

            //public cert - no sensitivity
            string tlscert = "2d2d2d2d2d424547494e2043455254494649434154452d2d2d2d2d0a4d494943417a434341617167417749424167494a4150486f4e765a39665942304d416f4743437147534d343942414d434d4430784c54417242674e5642414d4d0a4a474e766157357759573570597a45755a57467a6448567a4c6d4e736233566b595842774c6d463664584a6c4c6d4e766254454d4d416f4741315545436777440a6247356b4d434158445445344d444d794e4445354d5459774e6c6f59447a49784d5467774d6a49344d546b784e6a4132576a41394d5330774b775944565151440a4443526a62326c756347467561574d784c6d5668633352316379356a624739315a47467763433568656e56795a53356a623230784444414b42674e5642416f4d0a413278755a44425a4d424d4742797147534d34394167454743437147534d3439417745484130494142475169586c5970527766436648736e65694352627774430a4e774738656562437646786b344c6e4461584732684472305137394c465044376d34354271756f684937653531496f385073454c51644d4a2f2f686d6756616a0a675a41776759307744675944565230504151482f4241514441674b6b4d41384741315564457745422f7751464d414d4241663877616759445652305242474d770a5959496b59323970626e4268626d6c6a4d53356c59584e3064584d7559327876645752686348417559587031636d55755932397467676c7362324e68624768760a63335348424838414141474845414141414141414141414141414141414141414141474842416f4141415348455036414141414141414141416730362f2f34580a76344d77436759494b6f5a497a6a30454177494452774177524149674242735234374b592b6b777761456551565245634237703078472f41522b34446e7478770a6d794a633476454349446561797a6962437156357a795850392f624849485074637a51336c4148704b33546c62707932636c61410a2d2d2d2d2d454e442043455254494649434154452d2d2d2d2d0a";
            // h.EncodeData(m);
            //string TLSFilename = "tls.cert";
            X509Certificate2 certificates = new X509Certificate2();

            var tlsc = h.DecodeData(tlscert);
            certificates.Import(tlsc);

            var client = new RestClient("https://" + host + ":" + Convert.ToString(port));
            client.ClientCertificates = new X509CertificateCollection() { certificates };

            //X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            //store.Open(OpenFlags.ReadWrite);
            //store.Add(certificates);

            client.RemoteCertificateValidationCallback =
                delegate (object s, X509Certificate certificate,
                          X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    //TODO: fix later
                    return true;
                };

            var request = new RestRequest(restpath, Method.GET);
            request.AddHeader("Grpc-Metadata-macaroon", macaroon);

            //Execute Async
            client.ExecuteAsync(request, HandleResponse);
            //var response = client.Execute(request);
            //string responseStr = response.Content;
            //return responseStr;
        }

        private static void HandleResponse(IRestResponse response, RestRequestAsyncHandle h)
        {
            Console.WriteLine(response.Content);
        }

        private static string LndApiGet(string host, string restpath, int port = 8080, Dictionary<string, string> urlParameters = null, string macaroonAdmin = "", string macaroonRead = "")
        {
            string macaroon = "";
            if (macaroonRead != "")
            {
                macaroon = macaroonRead;
            }
            else if (macaroonAdmin != "")
            {
                macaroon = macaroonAdmin;
            }

            //X509Certificate2 certificates = new X509Certificate2();

            //var tls = System.IO.File.ReadAllBytes(TLSFilename);
            //var tlsh = h.EncodeData(tls);
            //var tlsc = h.DecodeData(tlsh);

            //certificates.Import(tlsc);

            var client = new RestClient("https://" + host + ":" + Convert.ToString(port));
            //client.ClientCertificates = new X509CertificateCollection() { certificates };

            //X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            //store.Open(OpenFlags.ReadWrite);
            //store.Add(certificates);

            client.RemoteCertificateValidationCallback =
                delegate (object s, X509Certificate certificate,
                          X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    //TODO: fix later
                    return true;
                };

            var request = new RestRequest(restpath, Method.GET);

            if (urlParameters != null)
            {
                foreach (var p in urlParameters)
                {
                    request.AddUrlSegment(p.Key, p.Value);
                }
            }

            request.AddHeader("Grpc-Metadata-macaroon", macaroon);

            var response = client.Execute(request);
            string responseStr = response.Content;
            return responseStr;
        }

        private static string LndApiPost(string host, string restpath, object body, int port = 8080)
        {
            string TLSFilename = "tls.cert";
            var m = System.IO.File.ReadAllBytes("readonly.macaroon");
            HexEncoder h = new HexEncoder();
            string macaroon = h.EncodeData(m);

            X509Certificate2 certificates = new X509Certificate2();

            var tls = System.IO.File.ReadAllBytes(TLSFilename);
            var tlsh = h.EncodeData(tls);

            var tlsc = h.DecodeData(tlsh);

            certificates.Import(tlsc);

            var client = new RestClient("https://" + host + ":" + Convert.ToString(port));
            client.ClientCertificates = new X509CertificateCollection() { certificates };

            //X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            //store.Open(OpenFlags.ReadWrite);
            //store.Add(certificates);

            client.RemoteCertificateValidationCallback =
                delegate (object s, X509Certificate certificate,
                          X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    //TODO: fix later
                    return true;
                };

            var request = new RestRequest(restpath, Method.POST);
            request.AddHeader("Grpc-Metadata-macaroon", macaroon);
            request.RequestFormat = DataFormat.Json;

            request.AddBody(body);

            var response = client.Execute(request);
            string responseStr = response.Content;
            return responseStr;
        }
    }
}