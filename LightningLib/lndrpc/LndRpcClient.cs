using NBitcoin.DataEncoders;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightningLib.lndrpc
{
    /* These are the event delegates used by the TransactionListener */
    public delegate void LnInvoicePaid(Invoice invoice);
    public delegate void LnStreamLost(TransactionListener sender);

    /// <summary>
    /// The Transaction Listener for /v1/invoices/subscribe
    /// </summary>
    public class TransactionListener
    {
        // This event is triggered when an invoice is paid 
        public event LnInvoicePaid InvoicePaid;

        // This event is triggered when the HTTP stream is closed
        public event LnStreamLost StreamLost;

        public bool IsLive = false;

        public Guid ListenerId = Guid.NewGuid();

        public string url;      // full url to /v1/invoices/subscribe
        public string macaroon; // used to call /v1/invoices/subscribe

        public async void Start()
        {
            using (var client = new HttpClient())
            {
                // The HTTP stream should stay open until closed by either server or client
                client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Grpc-Metadata-macaroon", macaroon);    // macaroon should be provided from lnd

//#if DEBUG
                // This disables SSL certificate validation
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => true;
//#endif
                using (var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead))
                {
                    using (var body = await response.Content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(body))
                    {
                        IsLive = true;
                        //need to read and chop message
                        try
                        {
                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                InvoiceEvent e = SimpleJson.SimpleJson.DeserializeObject<InvoiceEvent>(line);

                                //Notify listeners of new invoice
                                InvoicePaid?.Invoke(e.result);
                            }
                        }
                        catch (Exception e)
                        {
                            // TODO: check that the exception type is actually from a closed stream.
                            Debug.WriteLine(e.Message);
                            IsLive = false;
                            StreamLost?.Invoke(this);
                        }
                    }
                }
            }
        }
    }

    public class LndRpcClient
    {
        /// <summary>
        /// This should be set to the lnd host.  For example, "www.mywebsite.com", or "123.4.5.6"
        /// </summary>
        private string _host;
        private int _port;

        private string _macaroonRead = "";
        private string _macaroonAdmin = "";
        private string _macaroonInvoice = "";

        public string MacaroonAdmin { get => _macaroonAdmin; set => _macaroonAdmin = value; }
        public string MacaroonRead { get => _macaroonRead; set => _macaroonRead = value; }
        public string MacaroonInvoice { get => _macaroonInvoice; set => _macaroonInvoice = value; }

        public LndRpcClient(string host = "127.0.0.1", int port=8080, string macaroonAdmin="", string macaroonRead="", string macaroonInvoice = "")
        {
            _host = host;
            _macaroonAdmin = macaroonAdmin;
            _macaroonRead = macaroonRead;
            _macaroonInvoice = macaroonInvoice;
            _port = port;
        }

        /// <summary>
        /// Sets up a streaming listener for /v1/invoices/subscribe, but does not yet start it.
        /// </summary>
        /// <returns></returns>
        public TransactionListener GetListener()
        {
            var l = new TransactionListener()
            {
                url = "https://" + _host + ":" + Convert.ToString(_port) + "/v1/invoices/subscribe",
                macaroon = _macaroonAdmin,
            };
            return l;
        }

        public GetInfoResponse GetInfo()
        {
            return LndApiGetObj<GetInfoResponse>(_host, "/v1/getinfo", readMacaroon: _macaroonRead);
        }

        public GetChanInfoResponse GetChanInfo(string chanid)
        {
            return LndApiGetObj<GetChanInfoResponse>(_host, "/v1/graph/edge/{chan_id}",
                urlParameters: new Dictionary<string, string>() { { "chan_id", chanid } },
                readMacaroon: _macaroonRead);
        }

        public GetNodeInfoResponse GetNodeInfo(string pubkey)
        {
            return LndApiGetObj<GetNodeInfoResponse>(_host, "/v1/graph/node/{pub_key}",
                urlParameters: new Dictionary<string, string>() { { "pub_key", pubkey } },
                readMacaroon: _macaroonRead);
        }

        public SendPaymentResponse PayInvoice(string invoice)
        {
            var payreqParam = new { payment_request = invoice};
            return LndApiPost<SendPaymentResponse>(_host, "/v1/channels/transactions", payreqParam, adminMacaroon: _macaroonAdmin);
        }

        public GetChannelsResponse GetChannels()
        {
            return LndApiGetObj<GetChannelsResponse>(_host, "/v1/channels", readMacaroon: _macaroonRead);
        }

        public DecodePaymentResponse DecodePayment(string invoice)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                {"pay_req",  invoice},
            };
            return LndApiGetObj<DecodePaymentResponse>(_host, "/v1/payreq/{pay_req}", urlParameters: parameters, readMacaroon: _macaroonRead);
        }

        /// <summary>
        /// Create a new payment invoice.
        /// </summary>
        /// <param name="amount">Invoice amount in satoshi</param>
        /// <param name="memo">Plain text memo for invoice (records)</param>
        /// <returns></returns>
        public AddInvoiceResponse AddInvoice(Int64 amount_satoshi, string memo = "", string expiry="3600")
        {
            var invoice = new Invoice()
            {
                value = Convert.ToString(amount_satoshi),
                memo = memo,
                expiry = expiry,
            };
            return AddInvoice(invoice);
        }

        public AddInvoiceResponse AddInvoice(Invoice invoice)
        {
            // Posting to the invoices endpoint creates a new invoice
            return LndApiPost<AddInvoiceResponse>(_host, "/v1/invoices", invoice, adminMacaroon: _macaroonInvoice);
        }

        /// <summary>
        /// Return transaction routing events.  
        /// TODO: Configure proper pagination, as there is a limit in lnd once the number of forwarding events is large.
        /// </summary>
        /// <returns></returns>
        public ForwardingEventsResponse GetForwardingEvents()
        {
            var reqObj = new FwdRequest()
            {
                start_time = "0",
                end_time = "999999999999",  // Should be far enough in the future to get all of them up to the limit
                index_offset = 0,
                num_max_events = 50000,     // This is the max returned.  TODO: Need to update to do paging if more than 50k are returned
            };
            return LndApiPost<ForwardingEventsResponse>(_host, "/v1/switch", reqObj, adminMacaroon: _macaroonAdmin);
        }

        private static T LndApiPost<T>(string host, string restpath, object body, int port = 8080, string adminMacaroon = "") where T : new()
        {
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

            // TODO: put this into a .config file
            string tlscert = "2d2d2d2d2d424547494e2043455254494649434154452d2d2d2d2d0a4d494943417a434341617167417749424167494a4150486f4e765a39665942304d416f4743437147534d343942414d434d4430784c54417242674e5642414d4d0a4a474e766157357759573570597a45755a57467a6448567a4c6d4e736233566b595842774c6d463664584a6c4c6d4e766254454d4d416f4741315545436777440a6247356b4d434158445445344d444d794e4445354d5459774e6c6f59447a49784d5467774d6a49344d546b784e6a4132576a41394d5330774b775944565151440a4443526a62326c756347467561574d784c6d5668633352316379356a624739315a47467763433568656e56795a53356a623230784444414b42674e5642416f4d0a413278755a44425a4d424d4742797147534d34394167454743437147534d3439417745484130494142475169586c5970527766436648736e65694352627774430a4e774738656562437646786b344c6e4461584732684472305137394c465044376d34354271756f684937653531496f385073454c51644d4a2f2f686d6756616a0a675a41776759307744675944565230504151482f4241514441674b6b4d41384741315564457745422f7751464d414d4241663877616759445652305242474d770a5959496b59323970626e4268626d6c6a4d53356c59584e3064584d7559327876645752686348417559587031636d55755932397467676c7362324e68624768760a63335348424838414141474845414141414141414141414141414141414141414141474842416f4141415348455036414141414141414141416730362f2f34580a76344d77436759494b6f5a497a6a30454177494452774177524149674242735234374b592b6b777761456551565245634237703078472f41522b34446e7478770a6d794a633476454349446561797a6962437156357a795850392f624849485074637a51336c4148704b33546c62707932636c61410a2d2d2d2d2d454e442043455254494649434154452d2d2d2d2d0a";
            X509Certificate2 certificates = new X509Certificate2();

            var tlsc = h.DecodeData(tlscert);
            certificates.Import(tlsc);

            var client = new RestClient("https://" + host + ":" + Convert.ToString(port));
            client.ClientCertificates = new X509CertificateCollection() { certificates };

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
            IRestResponse<T> response = client.Execute<T>(request);
            T info = response.Data;
            return info;
        }

        public static string LndApiPostStr(string host, string restpath, object body, int port = 8080, string adminMacaroon = "")
        {
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

            string tlscert = "2d2d2d2d2d424547494e2043455254494649434154452d2d2d2d2d0a4d494943417a434341617167417749424167494a4150486f4e765a39665942304d416f4743437147534d343942414d434d4430784c54417242674e5642414d4d0a4a474e766157357759573570597a45755a57467a6448567a4c6d4e736233566b595842774c6d463664584a6c4c6d4e766254454d4d416f4741315545436777440a6247356b4d434158445445344d444d794e4445354d5459774e6c6f59447a49784d5467774d6a49344d546b784e6a4132576a41394d5330774b775944565151440a4443526a62326c756347467561574d784c6d5668633352316379356a624739315a47467763433568656e56795a53356a623230784444414b42674e5642416f4d0a413278755a44425a4d424d4742797147534d34394167454743437147534d3439417745484130494142475169586c5970527766436648736e65694352627774430a4e774738656562437646786b344c6e4461584732684472305137394c465044376d34354271756f684937653531496f385073454c51644d4a2f2f686d6756616a0a675a41776759307744675944565230504151482f4241514441674b6b4d41384741315564457745422f7751464d414d4241663877616759445652305242474d770a5959496b59323970626e4268626d6c6a4d53356c59584e3064584d7559327876645752686348417559587031636d55755932397467676c7362324e68624768760a63335348424838414141474845414141414141414141414141414141414141414141474842416f4141415348455036414141414141414141416730362f2f34580a76344d77436759494b6f5a497a6a30454177494452774177524149674242735234374b592b6b777761456551565245634237703078472f41522b34446e7478770a6d794a633476454349446561797a6962437156357a795850392f624849485074637a51336c4148704b33546c62707932636c61410a2d2d2d2d2d454e442043455254494649434154452d2d2d2d2d0a";
            X509Certificate2 certificates = new X509Certificate2();

            var tlsc = h.DecodeData(tlscert);
            certificates.Import(tlsc);

            var client = new RestClient("https://" + host + ":" + Convert.ToString(port));
            client.ClientCertificates = new X509CertificateCollection() { certificates };

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

        public static string LndApiGetStr(string host, string restpath, int port = 8080, Dictionary<string,string> urlParameters = null, string adminMacaroon = "")
        {
            //var m = System.IO.File.ReadAllBytes("readonly.macaroon");
            HexEncoder h = new HexEncoder();
            string macaroon = adminMacaroon;
            
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
            if (urlParameters != null)
            {
                foreach(var p in urlParameters)
                {
                    request.AddUrlSegment(p.Key, p.Value);
                }
            }
            request.AddHeader("Grpc-Metadata-macaroon", macaroon);
            var response = client.Execute(request);
            string responseStr = response.Content;
            return responseStr;
        }

        private static T LndApiGetObj<T>(string host, string restpath, int port = 8080, Dictionary<string, string> urlParameters = null, string readMacaroon = "") where T : new()
        {
            string macaroon = readMacaroon;
            //var m = System.IO.File.ReadAllBytes("readonly.macaroon");
            HexEncoder h = new HexEncoder();
            string tlscert = "2d2d2d2d2d424547494e2043455254494649434154452d2d2d2d2d0a4d494943417a434341617167417749424167494a4150486f4e765a39665942304d416f4743437147534d343942414d434d4430784c54417242674e5642414d4d0a4a474e766157357759573570597a45755a57467a6448567a4c6d4e736233566b595842774c6d463664584a6c4c6d4e766254454d4d416f4741315545436777440a6247356b4d434158445445344d444d794e4445354d5459774e6c6f59447a49784d5467774d6a49344d546b784e6a4132576a41394d5330774b775944565151440a4443526a62326c756347467561574d784c6d5668633352316379356a624739315a47467763433568656e56795a53356a623230784444414b42674e5642416f4d0a413278755a44425a4d424d4742797147534d34394167454743437147534d3439417745484130494142475169586c5970527766436648736e65694352627774430a4e774738656562437646786b344c6e4461584732684472305137394c465044376d34354271756f684937653531496f385073454c51644d4a2f2f686d6756616a0a675a41776759307744675944565230504151482f4241514441674b6b4d41384741315564457745422f7751464d414d4241663877616759445652305242474d770a5959496b59323970626e4268626d6c6a4d53356c59584e3064584d7559327876645752686348417559587031636d55755932397467676c7362324e68624768760a63335348424838414141474845414141414141414141414141414141414141414141474842416f4141415348455036414141414141414141416730362f2f34580a76344d77436759494b6f5a497a6a30454177494452774177524149674242735234374b592b6b777761456551565245634237703078472f41522b34446e7478770a6d794a633476454349446561797a6962437156357a795850392f624849485074637a51336c4148704b33546c62707932636c61410a2d2d2d2d2d454e442043455254494649434154452d2d2d2d2d0a";
            // h.EncodeData(m);, 
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
            if (urlParameters != null)
            {
                foreach (var p in urlParameters)
                {
                    request.AddUrlSegment(p.Key, p.Value);
                }
            }
            request.AddHeader("Grpc-Metadata-macaroon", macaroon);
            IRestResponse<T> response = client.Execute<T>(request);
            T info = response.Data;
            return info;
        }
    }
}
