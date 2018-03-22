using CoinController;
using coinpanic_airdrop.Database;
using coinpanic_airdrop.Models;
using CoinpanicLib.Models;
using CoinpanicLib.NodeConnection;
using NBitcoin;
using NBitcoin.Forks;
using RestSharp;
using shortid;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static CoinpanicLib.Services.MailingService;

namespace coinpanic_airdrop.Controllers
{
    public class ClaimController : Controller
    {

        private CoinpanicContext db = new CoinpanicContext();

        // GET: NewClaim
        public async Task<ActionResult> NewClaim(string coin, string coupon)
        {
            string claimId = ShortId.Generate(useNumbers: false, useSpecial: false, length: 10);
            string ip = Request.UserHostAddress;

            //ensure unique
            while (db.Claims.Where(c => c.ClaimId == claimId).Count() > 0)
                claimId = ShortId.Generate(useNumbers: false, useSpecial: false, length: 10);

            db.Claims.Add(new CoinClaim()
            {
                ClaimId = claimId,
                CoinShortName = coin,
                RequestIP = ip
            });
            var res = await db.SaveChangesAsync();

            if (BitcoinForks.ForkByShortName.Keys.Contains(coin))
            {
                var NewClaim = new CoinClaim { CoinShortName = coin, ClaimId = claimId };

                return View(NewClaim);
            }
            else
            {
                return RedirectToAction("InvalidCoin");
            }
            
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult InitializeClaim(string claimId, string PublicKeys, string depositAddress, string emailAddress)
        {
            var userclaim = db.Claims.Where(c => c.ClaimId == claimId).Include(c => c.InputAddresses).First();

            //clean up
            depositAddress = depositAddress.Replace("\n", String.Empty);
            depositAddress = depositAddress.Replace("\r", String.Empty);
            depositAddress = depositAddress.Replace("\t", String.Empty);
            depositAddress = depositAddress.Trim().Replace(" ", "");

            userclaim.DepositAddress = depositAddress;
            userclaim.Email = emailAddress;

            List<string> list = new List<string>(
                           PublicKeys.Split(new string[] { "\r\n" },
                           StringSplitOptions.RemoveEmptyEntries));

            if (list.Count < 1)
                return RedirectToAction("ClaimError", new { message = "You must enter at least one address to claim", claimId = claimId });
            
            if (!Bitcoin.IsValidAddress(depositAddress, userclaim.CoinShortName))
                return RedirectToAction("ClaimError", new { message = "Deposit Address not valid", claimId = claimId });

            var invalid = list.Where(a => !Bitcoin.IsValidAddress(a));
            if (invalid.Count() > 0)
            {
                return RedirectToAction("ClaimError", new { message = String.Join(", ",invalid) + (invalid.Count() < 2 ? " is" : " are") + " invalid.", claimId = claimId });
            }

            var scanner = new BlockScanner();
            var claimAddresses = Bitcoin.ParseAddresses(list);

            Tuple<List<ICoin>, Dictionary<string, double>> claimcoins;
            try
            { 
                claimcoins = scanner.GetUnspentTransactionOutputs(claimAddresses, userclaim.CoinShortName);
            }
            catch (Exception e)
            {
                return RedirectToAction("ClaimError", new { message = "Error searching for your addresses in the blockchain", claimId = claimId });
            }

            var amounts = scanner.CalculateOutputAmounts_Their_My_Fee(claimcoins.Item1, 0.05, 0.0003 * claimcoins.Item1.Count);
            var balances = claimcoins.Item2;

            List<InputAddress> inputs;
            if (userclaim.CoinShortName == "BTCP")
            {
                inputs = list.Select(li => new InputAddress()
                {
                    AddressId = Guid.NewGuid(),
                    PublicAddress = li + " -> " + Bitcoin.ParseAddress(li).Convert(Network.BTCP).ToString(),
                    CoinShortName = userclaim.CoinShortName,
                    ClaimId = userclaim.ClaimId,
                    ClaimValue = balances[li],
                }).ToList();
            }
            else
            {
                inputs = list.Select(li => new InputAddress()
                    {
                        AddressId = Guid.NewGuid(),
                        PublicAddress = li,
                        CoinShortName = userclaim.CoinShortName,
                        ClaimId = userclaim.ClaimId,
                        ClaimValue = balances[li],
                    }).ToList();
            }
            

            userclaim.InputAddresses = inputs;
            userclaim.Deposited = Convert.ToDouble(amounts[0].ToDecimal(MoneyUnit.BTC));
            userclaim.MyFee = Convert.ToDouble(amounts[1].ToDecimal(MoneyUnit.BTC));
            userclaim.MinerFee = Convert.ToDouble(amounts[2].ToDecimal(MoneyUnit.BTC));
            userclaim.TotalValue = userclaim.Deposited + userclaim.MyFee + userclaim.MinerFee;
            userclaim.InitializeDate = DateTime.Now;

            if (userclaim.Deposited < 0)
                userclaim.Deposited = 0;
            if (userclaim.MyFee < 0)
                userclaim.MyFee = 0;

            // Generate unsigned tx
            var mydepaddr = ConfigurationManager.AppSettings[userclaim.CoinShortName + "Deposit"];

            var utx = Bitcoin.GenerateUnsignedTX(claimcoins.Item1, amounts, Bitcoin.ParseAddress(userclaim.DepositAddress, userclaim.CoinShortName),
                Bitcoin.ParseAddress(mydepaddr, userclaim.CoinShortName),
                userclaim.CoinShortName);

            userclaim.UnsignedTX = utx;

            // Generate witness data
            var w = Bitcoin.GetBlockData(claimcoins.Item1);
            userclaim.BlockData = w;

            // New format of message
            BlockData bd = new BlockData()
            {
                fork = userclaim.CoinShortName,
                coins = claimcoins.Item1,
                utx = utx,
                addresses = balances.Select(kvp => kvp.Key).ToList(),
            };
            string bdstr = NBitcoin.JsonConverters.Serializer.ToString(bd);
            userclaim.ClaimData = bdstr;

            db.SaveChanges();

            MonitoringService.SendMessage("New " + userclaim.CoinShortName + " claim", "new claim Initialized. https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + claimId + " " + " for " + userclaim.CoinShortName);

            ViewBag.NumConnectedNodes = CoinPanicServer.GetNumNodes(userclaim.CoinShortName);

            return RedirectToAction("ClaimConfirm", new { claimId = claimId });
        }

        public ActionResult ClaimConfirm(string claimId)
        {
            try
            {
                var userclaim = db.Claims.Where(c => c.ClaimId == claimId).Include(c => c.InputAddresses).First();
                ViewBag.NumConnectedNodes = CoinPanicServer.GetNumNodes(userclaim.CoinShortName);
                ViewBag.Multiplier = BitcoinForks.ForkByShortName[userclaim.CoinShortName].Multiplier;
                return View(userclaim);
            }
            catch
            {
                return RedirectToAction("ClaimError", new { message = "claimId not valid", claimId = claimId });
            }
        }

        public ActionResult ClaimError(string message, string claimId)
        {
            ViewBag.Title = "Claim Error";
            ViewBag.message = message;
            ViewBag.ClaimId = claimId;
            return View();
        }

        public ActionResult InvalidCoin()
        {
            return View();
        }

        public ActionResult DownloadTransactionFile(string claimId)
        {
            var userclaim = db.Claims.Where(c => c.ClaimId == claimId);

            if (userclaim.Count() < 1)
            {
                return RedirectToAction("ClaimError", new { message = "Unable to find data for claim " + claimId });
            }

            Response.Clear();
            Response.AddHeader("Content-Disposition", "attachment; filename=BlockChainData.txt");
            Response.ContentType = "text/json";

            // Write all my data
            string blockdata = userclaim.First().BlockData;
            Response.Write(blockdata);
            Response.End();

            // Not sure what else to do here
            return Content(String.Empty);
        }

        public ActionResult DownloadClaimDataFile(string claimId)
        {
            var userclaim = db.Claims.Where(c => c.ClaimId == claimId);

            if (userclaim.Count() < 1)
            {
                return RedirectToAction("ClaimError", new { message = "Unable to find data for claim " + claimId });
            }

            Response.Clear();
            Response.AddHeader("Content-Disposition", "attachment; filename=ClaimData.txt");
            Response.ContentType = "text/json";

            // Write all my data
            string blockdata = userclaim.First().ClaimData;
            Response.Write(blockdata);
            Response.End();

            // Not sure what else to do here
            return Content(String.Empty);
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult RefreshNode(string coin)
        {
            if (coin == null)
                return RedirectToAction("ClaimError", new { message = "Provide a coin parameter", claimId = "" });

            if (!CoinPanicServer.IsInitialized)
            {
                InitializeNodes();
            }
            else
            {
                // List of seed nodes
                var seedNodesFromDb = db.SeedNodes.Where(n => n.Coin == coin).ToList();

                var seednodes = seedNodesFromDb.Select(n => new NodeDetails()
                {
                    coin = n.Coin,
                    ip = n.IP,
                    port = n.Port,
                    use = n.Enabled,
                }).ToList();

                CoinPanicServer.emailhost = System.Configuration.ConfigurationManager.AppSettings["EmailSMTPHost"];
                CoinPanicServer.emailport = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["EmailSMTPPort"]);
                CoinPanicServer.emailuser = System.Configuration.ConfigurationManager.AppSettings["EmailUser"];
                CoinPanicServer.emailpass = System.Configuration.ConfigurationManager.AppSettings["EmailPass"];

                CoinPanicServer.InitializeNodes(seednodes);
            }
            
            return RedirectToAction("CheckNode", new { coin = coin });
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult CheckNode(string coin)
        {
            if (coin == null)
                return RedirectToAction("ClaimError", new { message = "Provide a coin parameter", claimId = "" });

            if (!CoinPanicServer.IsInitialized)
            {
                InitializeNodes();
            }

            var svr = CoinPanicServer.GetNodeServer(coin);
            List<NodeStatus> nodestatus = new List<NodeStatus>();

            foreach(var n in svr.ConnectedNodes)
            {
                nodestatus.Add(new NodeStatus()
                {
                
                    IP = (n.State == NBitcoin.Protocol.NodeState.HandShaked || n.State == NBitcoin.Protocol.NodeState.Connected) ? n.Peer.Endpoint.Address.ToString(): "",
                    name = (n.State == NBitcoin.Protocol.NodeState.HandShaked || n.State == NBitcoin.Protocol.NodeState.Connected) ? n.PeerVersion.UserAgent : "",
                    port = (n.State == NBitcoin.Protocol.NodeState.HandShaked || n.State == NBitcoin.Protocol.NodeState.Connected) ? Convert.ToString(n.Peer.Endpoint.Port) : "",
                    Status = n.State.ToString(),
                    uptime = (n.State == NBitcoin.Protocol.NodeState.HandShaked || n.State == NBitcoin.Protocol.NodeState.Connected) ? n.Peer.Ago.ToString() : "",//n.Counter.Start.ToUniversalTime().ToShortDateString() + " " +n.Counter.Start.ToUniversalTime().ToLongTimeString(): "",
                    version = n.State == NBitcoin.Protocol.NodeState.HandShaked ? Convert.ToString(n.PeerVersion.Version) : "",
                });
            }
            NodesStatus res = new NodesStatus() { Nodes = nodestatus };

            return View(res);
        }

        private void InitializeNodes()
        {
            // List of seed nodes
            var seedNodesFromDb = db.SeedNodes.ToList();

            var seednodes = seedNodesFromDb.Select(n => new NodeDetails()
            {
                coin = n.Coin,
                ip = n.IP,
                port = n.Port,
                use = n.Enabled,
            }).ToList();

            CoinPanicServer.emailhost = System.Configuration.ConfigurationManager.AppSettings["EmailSMTPHost"];
            CoinPanicServer.emailport = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["EmailSMTPPort"]);
            CoinPanicServer.emailuser = System.Configuration.ConfigurationManager.AppSettings["EmailUser"];
            CoinPanicServer.emailpass = System.Configuration.ConfigurationManager.AppSettings["EmailPass"];

            CoinPanicServer.InitializeNodes(seednodes);
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult AddServerNode(string coin, string ip, int port)
        {
            if (!CoinPanicServer.IsInitialized)
            {
                InitializeNodes();
            }

            var newsn = new SeedNode()
            {
                Coin = coin,
                IP = ip,
                Enabled = true,
                Port = port,
            };

            if (!db.SeedNodes.Any(n => (n.IP == ip) && (n.Coin == coin)))
            {
                db.SeedNodes.Add(newsn);
                db.SaveChanges();
            }

            List<SeedNode> sn = new List<SeedNode>() { newsn };
            
            //map to server format
            var initnodes = sn.Select(n => new NodeDetails()
            {
                coin = n.Coin,
                ip = n.IP,
                port = n.Port,
                use = n.Enabled,
            }).ToList();

            CoinPanicServer.InitializeNodes(initnodes);

            return RedirectToAction("CheckNode", new { coin = coin });
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult TransmitTransaction(string claimId, string signedTransaction)
        {
            if (!CoinPanicServer.IsInitialized)
            {
                InitializeNodes();
            }

            var userclaim = db.Claims.Where(c => c.ClaimId == claimId).First();
            ViewBag.content = userclaim.CoinShortName + " not currently supported.";
            ViewBag.ClaimId = claimId;
            signedTransaction = signedTransaction.Replace("\n", String.Empty);
            signedTransaction = signedTransaction.Replace("\r", String.Empty);
            signedTransaction = signedTransaction.Replace("\t", String.Empty);
            userclaim.SignedTX = signedTransaction.Trim().Replace(" ", "");
            db.SaveChanges();

            if (userclaim.UnsignedTX == userclaim.SignedTX)
            {
                return RedirectToAction("ClaimError", new { message = "Transaction was not signed.  Check that you have the newest BlockChainData.txt and correct private keys.", claimId = claimId });
            }

            Transaction t;
            try
            { 
                t = Transaction.Parse(signedTransaction.Trim().Replace(" ", ""));
            }
            catch (Exception e)
            {
                MonitoringService.SendMessage("Invalid tx " + userclaim.CoinShortName + " submitted " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + claimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                return RedirectToAction("ClaimError", new { message = e.Message + ". Unable to parse signed transaction: \r\n" + signedTransaction, claimId = claimId });
            }
            string txid = t.GetHash().ToString();
            userclaim.TransactionHash = txid;
            db.SaveChanges();
            
            if (true)
            //if (userclaim.CoinShortName == "B2X")
            //{
            //    userclaim.SignedTX = signedTransaction;
            //    var client = new RestClient("http://explorer.b2x-segwit.io/b2x-insight-api/");
            //    var request = new RestRequest("tx/send/", Method.POST);
            //    request.AddJsonBody(new { rawtx = signedTransaction });
            //    //request.AddParameter("rawtx", signedTransaction);

            //    IRestResponse response = client.Execute(request);
            //    var content = response.Content; // raw content as string
            //    ViewBag.content = content;
            //    userclaim.TransactionHash = content;
            //    userclaim.WasTransmitted = true;
            //    MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + claimId + " " + " for " + userclaim.CoinShortName + "\r\n txid: " + txid);

            //    db.SaveChanges();
            //}
            //else if (userclaim.CoinShortName == "BTG")
            //{
            //    userclaim.SignedTX = signedTransaction;
            //    var client = new RestClient(" https://btgexplorer.com/api/");
            //    var request = new RestRequest("tx/send", Method.POST);
            //    request.AddJsonBody(new { rawtx = signedTransaction });

            //    IRestResponse response = client.Execute(request);
            //    var content = response.Content; // raw content as string
            //    ViewBag.content = content;
            //    userclaim.TransactionHash = content;
            //    userclaim.WasTransmitted = true;
            //    MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + claimId + " " + " for " + userclaim.CoinShortName + "\r\n txid: " + txid);

            //    db.SaveChanges();
            //}
            //else
            {
                //broadcast it directly to a node
                var txed = CoinPanicServer.BroadcastTransaction(coin: userclaim.CoinShortName, transaction: t);
                if (!txed.IsError)
                {
                    //broadcasted
                    ViewBag.content = txed.Result + " your transaction id is: " + txid;
                    userclaim.TransactionHash = txid;
                    userclaim.WasTransmitted = true;
                    userclaim.SignedTX = signedTransaction;
                    MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + claimId + " " + " for " + userclaim.CoinShortName + "\r\n txid: " + txid + "\r\nResult: " + txed.Result);
                }
                else
                {
                    ViewBag.content = "Error: " + txed.Result;
                    userclaim.WasTransmitted = false;
                    userclaim.SignedTX = signedTransaction;
                    MonitoringService.SendMessage("New " + userclaim.CoinShortName + " error broadcasting " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + claimId + " " + " for " + userclaim.CoinShortName + "\r\n txid: " + txid + "\r\nResult: " + txed.Result);
                }
                db.SaveChanges();
            }
            db.SaveChanges();
            //
            //else if (userclaim.CoinShortName == "BCX")  //Bitcoin Faith
            //{
            //    //https://www.coinpanic.com/Claim/ClaimConfirm?claimId=kIXqSkIskQ
            //    userclaim.SignedTX = signedTransaction;

            //    List<string> nodeips = new List<string>()
            //    {
            //        "192.169.153.174",
            //        "192.169.154.185",
            //        "120.131.13.249",
            //    };
            //    List<string> results = new List<string>();
            //    string result = "";
            //    bool success = false;
            //    foreach (string nip in nodeips)
            //    {
            //        var BitcoinNode = new BitcoinNode(address: nip, port: 9003);
            //        try
            //        {
            //            result = BitcoinNode.BroadcastTransaction(transaction, Forks.ForkShortNameCode[userclaim.CoinShortName]);

            //            if (result == transaction.GetHash().ToString() )
            //            {
            //                success = true;
            //                break;
            //            }
            //            else if(result.Substring(0, 6) == "Reject")
            //            {
            //                success = false;
            //                break;
            //            }
            //            else
            //            {
            //                results.Add(result);
            //            }
            //        }
            //        catch (Exception e)
            //        {
            //            results.Add(nip + ":" + e.Message);
            //        }
            //    }
            //    if (success)
            //    {
            //        ViewBag.content = "Coins successfully broadcast.  Your transaction is: " + transaction.GetHash().ToString();
            //        userclaim.TransactionHash = transaction.GetHash().ToString();
            //        userclaim.WasTransmitted = true;
            //        userclaim.SignedTX = signedTransaction;
            //    }
            //    else
            //    {
            //        ViewBag.content = "Error broadcasting your transaction: " + String.Join(";", results.ToArray());
            //        userclaim.WasTransmitted = false;
            //        userclaim.SignedTX = signedTransaction;
            //    }
            //    db.SaveChanges();
            //}
            //else if (userclaim.CoinShortName == "BTF")  //Bitcoin Faith
            //{
            //    //http://localhost:53483/Claim/ClaimConfirm?claimId=YdJGSDTzfN
            //    userclaim.SignedTX = signedTransaction;
            //    Transaction transaction = Transaction.Parse(signedTransaction);
            //    List<string> nodeips = new List<string>()
            //    {
            //        "47.90.38.149",
            //        "120.55.126.189",
            //        "47.90.16.179",
            //        "47.90.38.158",
            //        "47.90.37.123", //b.btf.hjy.cc
            //        "47.90.62.100",
            //    };
            //    //port 8346
            //    List<string> results = new List<string>();
            //    string result = "";
            //    bool success = false;
            //    foreach (string nip in nodeips)
            //    {
            //        var BitcoinNode = new BitcoinNode(address: nip, port: 8346);
            //        try
            //        {
            //            result = BitcoinNode.BroadcastTransaction(transaction, Forks.ForkShortNameCode[userclaim.CoinShortName]);

            //            if (result == transaction.GetHash().ToString())
            //            {
            //                success = true;
            //                break;
            //            }
            //            else
            //            {
            //                results.Add(result);
            //            }
            //        }
            //        catch (Exception e)
            //        {
            //            results.Add(nip + ":" + e.Message);
            //        }
            //    }
            //    if (success)
            //    {
            //        ViewBag.content = "Coins successfully broadcast.  Your transaction is: " + transaction.GetHash().ToString();
            //        userclaim.TransactionHash = transaction.GetHash().ToString();
            //        userclaim.WasTransmitted = true;
            //        userclaim.SignedTX = signedTransaction;
            //    }
            //    else
            //    {
            //        ViewBag.content = "Error broadcasting your transaction: " + String.Join(";", results.ToArray());
            //        userclaim.WasTransmitted = false;
            //        userclaim.SignedTX = signedTransaction;
            //    }
            //    db.SaveChanges();
            //}
            //else if (userclaim.CoinShortName == "SBTC") //Super bitcoin
            //{
            //    userclaim.SignedTX = signedTransaction;
            //    Transaction transaction = Transaction.Parse(signedTransaction);

            //    List<string> nodeips = new List<string>()
            //    {
            //        "185.17.31.58",
            //        "162.212.157.232",
            //        "101.201.117.68",
            //        "162.212.157.232",
            //        "123.56.143.216"
            //    };
            //    List<string> results = new List<string>();
            //    string result = "";
            //    bool success = false;
            //    foreach (string nip in nodeips)
            //    {
            //        try
            //        {


            //            var BitcoinNode = new BitcoinNode(address: nip, port: 8334);
            //            result = BitcoinNode.BroadcastTransaction(transaction, Forks.ForkShortNameCode[userclaim.CoinShortName]);

            //            if (result == transaction.GetHash().ToString())
            //            {
            //                success = true;
            //                break;
            //            }
            //            else
            //            {
            //                results.Add(result);
            //            }
            //        }
            //        catch (Exception e)
            //        {
            //            results.Add(nip + ":" + e.Message);
            //        }
            //    }
            //    if (success)
            //    {
            //        ViewBag.content = "Coins successfully broadcast.  Your transaction is: " + transaction.GetHash().ToString();
            //        userclaim.TransactionHash = transaction.GetHash().ToString();
            //        userclaim.WasTransmitted = true;
            //        userclaim.SignedTX = signedTransaction;
            //    }
            //    else
            //    {
            //        ViewBag.content = "Error broadcasting your transaction: " + String.Join(";", results.ToArray());
            //        userclaim.WasTransmitted = false;
            //        userclaim.SignedTX = signedTransaction;
            //    }
            //    db.SaveChanges();
            //}

            return View();
        }

        
    }
}

/*
.controller(
 "SendRawTransactionController",
 function($scope,$http,Api)
 {
    $scope.transaction="",
    $scope.status="ready",
    $scope.txid="",
    $scope.error=null,
    $scope.formValid=function()
    {
        return!!$scope.transaction
    },
    $scope.send=function()
    {
        var postData={rawtx:$scope.transaction};
        $scope.status="loading",
        $http.post(Api.apiPrefix+"/tx/send",postData)
            .success(
                function(data,status,headers,config)
                {
                    return"string"!=typeof data.txid?($scope.status="error",void($scope.error="The transaction was sent but no transaction id was got back")):($scope.status="sent",void($scope.txid=data.txid))
                })
            .error(
                function(data,status,headers,config)
                {
                    $scope.status="error",
                    data?$scope.error=data:$scope.error="No error message given (connection error?)"
                })
    }
 })

*/
