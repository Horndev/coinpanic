using CoinController;
using coinpanic_airdrop.Database;
using CoinpanicLib.Models;
using CoinpanicLib.NodeConnection.Api;
using NBitcoin;
using NBitcoin.Forks;
using RestSharp;
using RestSharp.Authenticators;
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
        // GET: NewClaim
        public async Task<ActionResult> NewClaim(string coin, string coupon, string addresses)
        {
            string claimId = ShortId.Generate(useNumbers: false, useSpecial: false, length: 10);
            string ip = Request.UserHostAddress;

            List<string> addressList = addresses != null ? new List<string>(
                addresses.Split(new string[] { "," },
                StringSplitOptions.RemoveEmptyEntries)) : new List<string>();

            using (CoinpanicContext db = new CoinpanicContext())
            {
                // Ensure the ClaimId is unique
                while (db.Claims.Where(c => c.ClaimId == claimId).Count() > 0)
                    claimId = ShortId.Generate(useNumbers: false, useSpecial: false, length: 10);

                db.Claims.Add(new CoinClaim()
                {
                    ClaimId = claimId,
                    CoinShortName = coin,
                    RequestIP = ip
                });
                var res = db.SaveChanges();
                var ci = db.IndexCoinInfo.Where(i => i.Coin == coin).AsNoTracking();
                if (ci.Count() > 0)
                {
                    ViewBag.Exchanges = ci.First().Exchanges.ToList();
                }
                else
                {
                    ViewBag.Exchanges = new List<Models.ExchangeInfo>();
                }
            }

            

            // Make sure we understand how to sign the requested coin
            if (BitcoinForks.ForkByShortName.Keys.Contains(coin))
            {
                var NewClaim = new CoinClaim { CoinShortName = coin, ClaimId = claimId };
                List<InputAddress> inputs;
                inputs = GetInputAddresses(NewClaim, addressList);
                NewClaim.InputAddresses = inputs;
                return View(NewClaim);
            }
            else
            {
                return RedirectToAction("InvalidCoin");
            }
        }

        [HttpPost, AllowAnonymous]
        public ActionResult InitializeClaim(string claimId, string PublicKeys, string depositAddress, string emailAddress)
        {
            using (CoinpanicContext db = new CoinpanicContext())
            {
                CoinClaim userclaim = db.Claims.Where(c => c.ClaimId == claimId).Include(c => c.InputAddresses).First();

                // Clean up input
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

                // Convert depositAddress if BCH

                if (userclaim.CoinShortName == "BCH" && depositAddress.StartsWith("bitcoincash:"))
                {
                    // Convert
                    depositAddress = SharpCashAddr.Converter.cashAddrToOldAddr(depositAddress, out bool isP2PKH, out _);
                }

                if (!Bitcoin.IsValidAddress(depositAddress, userclaim.CoinShortName, BitcoinForks.ForkByShortName[userclaim.CoinShortName].Network))
                    return RedirectToAction("ClaimError", new { message = "Deposit Address not valid", claimId = claimId });

                var invalid = list.Where(a => !Bitcoin.IsValidAddress(a));
                if (invalid.Count() > 0)
                {
                    return RedirectToAction("ClaimError", new { message = String.Join(", ", invalid) + (invalid.Count() < 2 ? " is" : " are") + " invalid.", claimId = claimId });
                }

                var scanner = new BlockScanner();
                var claimAddresses = Bitcoin.ParseAddresses(list);

                Tuple<List<ICoin>, Dictionary<string, double>> claimcoins;
                try
                {
                    claimcoins = scanner.GetUnspentTransactionOutputs(claimAddresses, userclaim.CoinShortName, out bool usedExplorer);
                }
                catch (Exception e)
                {
                    return RedirectToAction("ClaimError", new { message = "Error searching for your addresses in the blockchain", claimId = claimId });
                }

                var amounts = scanner.CalculateOutputAmounts_Their_My_Fee(claimcoins.Item1, 0.05, 0.0003 * claimcoins.Item1.Count);
                var balances = claimcoins.Item2;

                // Return converted addresses to user
                List<InputAddress> inputs;
                inputs = GetInputAddresses(userclaim, list, balances);

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

                if (userclaim.CoinShortName == "BCH" && mydepaddr.StartsWith("bitcoincash:"))
                {
                    // Convert
                    mydepaddr = SharpCashAddr.Converter.cashAddrToOldAddr(mydepaddr, out bool isP2PKH, out _);
                }

                var utx = Bitcoin.GenerateUnsignedTX(
                    UTXOs: claimcoins.Item1,
                    amounts: amounts,
                    clientDepAddr: Bitcoin.ParseAddress(depositAddress, userclaim.CoinShortName, BitcoinForks.ForkByShortName[userclaim.CoinShortName].Network),
                    MyDepositAddr: Bitcoin.ParseAddress(mydepaddr, userclaim.CoinShortName, BitcoinForks.ForkByShortName[userclaim.CoinShortName].Network),
                    forkShortName: userclaim.CoinShortName);

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
            }
            return RedirectToAction("ClaimConfirm", new { claimId = claimId });
        }

        private static List<InputAddress> GetInputAddresses(CoinClaim userclaim, List<string> list, Dictionary<string, double> balances = null)
        {
            List<InputAddress> inputs;
            if (userclaim.CoinShortName == "BTCP")
            {
                inputs = list.Select(li => new InputAddress()
                {
                    AddressId = Guid.NewGuid(),
                    PublicAddress = li + " -> " + Bitcoin.ParseAddress(li).Convert(Network.BTCP).ToString(),
                    CoinShortName = userclaim.CoinShortName,
                    ClaimId = userclaim.ClaimId,
                    ClaimValue = balances != null ? balances[li] : -1,
                }).ToList();
            }
            else if (userclaim.CoinShortName == "BCI")
            {
                inputs = list.Select(li => new InputAddress()
                {
                    AddressId = Guid.NewGuid(),
                    PublicAddress = li + " -> " + Bitcoin.ParseAddress(li).Convert(Network.BCI).ToString(),
                    CoinShortName = userclaim.CoinShortName,
                    ClaimId = userclaim.ClaimId,
                    ClaimValue = balances != null ? balances[li] : -1,
                }).ToList();
            }
            else if (userclaim.CoinShortName == "BCA")
            {
                inputs = list.Select(li => new InputAddress()
                {
                    AddressId = Guid.NewGuid(),
                    PublicAddress = li + " -> " + Bitcoin.ParseAddress(li).Convert(Network.BCA).ToString(),
                    CoinShortName = userclaim.CoinShortName,
                    ClaimId = userclaim.ClaimId,
                    ClaimValue = balances != null ? balances[li] : -1,
                }).ToList();
            }
            else if (userclaim.CoinShortName == "BTG")
            {
                inputs = list.Select(li => new InputAddress()
                {
                    AddressId = Guid.NewGuid(),
                    PublicAddress = li + " -> " + Bitcoin.ParseAddress(li).Convert(Network.BTG).ToString(),
                    CoinShortName = userclaim.CoinShortName,
                    ClaimId = userclaim.ClaimId,
                    ClaimValue = balances != null ? balances[li] : -1,
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
                    ClaimValue = balances != null ? balances[li] : -1,
                }).ToList();
            }

            return inputs;
        }

        /// <summary>
        /// Controller for the claim confirmation page, where users will
        /// review the claim and get instructions for signing.
        /// </summary>
        /// <param name="claimId"></param>
        /// <returns></returns>
        public ActionResult ClaimConfirm(string claimId)
        {
            try
            {
                CoinClaim userclaim = new CoinClaim();
                using (CoinpanicContext db = new CoinpanicContext())
                {
                    userclaim = db.Claims.Where(c => c.ClaimId == claimId).Include(c => c.InputAddresses).AsNoTracking().First();
                }
                ViewBag.Multiplier = BitcoinForks.ForkByShortName[userclaim.CoinShortName].Multiplier;

                //if (userclaim.CoinShortName == "BCH")
                //{
                //    // Convert
                //    var depositAddress = SharpCashAddr.Converter.oldAddrToCashAddr(userclaim.DepositAddress, out bool isP2PKH, out _);
                //    userclaim.DepositAddress = depositAddress;
                //}

                return View(userclaim);
            }
            catch
            {
                return RedirectToAction("ClaimError", new { message = "claimId not valid", claimId = claimId });
            }
        }

        /// <summary>
        /// Broadcast a claim to the blockchain
        /// </summary>
        /// <param name="ClaimId">Database identifier</param>
        /// <param name="Hex">Hex encoded transaction</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Broadcast(string ClaimId, string Hex)
        {
            /*
             * Known Errors:
             * 
             * Missing a signature on one or more input (private key)
             * {"result":null,"error":{"code":-26,"message":"16: mandatory-script-verify-flag-failed (Operation not valid with the current stack size)"},"id":"1"}
             */
            using (CoinpanicContext db = new CoinpanicContext())
            {
                CoinClaim userclaim = db.Claims.Where(c => c.ClaimId == ClaimId).FirstOrDefault();

                if (userclaim == null)
                {
                    userclaim = new CoinClaim();
                }

                //Clean up the signed transaction Hex
                string signedTransaction = Hex;
                signedTransaction = signedTransaction.Replace("\n", String.Empty);
                signedTransaction = signedTransaction.Replace("\r", String.Empty);
                signedTransaction = signedTransaction.Replace("\t", String.Empty);
                signedTransaction = signedTransaction.Trim().Replace(" ", "");
                userclaim.SignedTX = signedTransaction;
                userclaim.SubmitDate = DateTime.Now;
                if (signedTransaction != "")
                {
                    db.SaveChanges();
                }

                // This is what is returned to the client
                BroadcastResponse response = new BroadcastResponse()
                {
                    Error = false,
                    Result = "Transaction successfully broadcast.",
                    Txid = "",
                };

                var tx = signedTransaction;

                if (tx == "")
                {
                    response.Error = true;
                    response.Result = "Error: No signed transaction provided.";
                    MonitoringService.SendMessage("Empty tx " + userclaim.CoinShortName + " submitted.",
                        "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId);
                    return Json(response);
                }

                Transaction t = null;
                try
                {
                    t = Transaction.Parse(tx.Trim().Replace(" ", ""));
                }
                catch (Exception e)
                {
                    response.Error = true;
                    response.Result = "Error parsing transaction";
                    MonitoringService.SendMessage("Invalid tx " + userclaim.CoinShortName + " submitted " + Convert.ToString(userclaim.TotalValue),
                        "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " 
                        + signedTransaction + "\r\n"
                        + e.Message);
                    return Json(response);
                }

                // Transmit via explorers
                if (userclaim.CoinShortName == "BCA")
                {
                    try
                    {
                        var url = "https://explorer.bitcoinatom.io/";
                        var client = new RestClient(url);
                        var request = new RestRequest("/rpc-terminal/", Method.POST);
                        request.AddHeader("content-type", "application/x-www-form-urlencoded");
                        request.AddHeader("x-requested-with", "XMLHttpRequest");
                        request.AddObject(new
                        {
                            cmd = "sendrawtransaction " + signedTransaction
                        });

                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;

                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                            "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction
                            + "\r\n Result: " + content);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }
                if (userclaim.CoinShortName == "B2X")
                {
                    try
                    {
                        var client = new RestClient("https://explorer.b2x-segwit.io/b2x-insight-api/");
                        var request = new RestRequest("tx/send/", Method.POST);
                        request.AddJsonBody(new { rawtx = signedTransaction });

                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;

                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                            "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction
                            + "\r\n Result: " + content);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }
                if (userclaim.CoinShortName == "SBTC")
                {
                    //http://block.superbtc.org/insight-api/tx/send
                    try
                    {
                        var client = new RestClient("http://block.superbtc.org/insight-api/");
                        var request = new RestRequest("tx/send/", Method.POST);
                        request.AddJsonBody(new { rawtx = signedTransaction });

                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;

                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                            "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction
                            + "\r\n Result: " + content);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }
                if (userclaim.CoinShortName == "BCI")
                {
                    //https://explorer.bitcoininterest.io/api/
                    try
                    {
                        var client = new RestClient("https://explorer.bitcoininterest.io/api/");
                        var request = new RestRequest("tx/send/", Method.POST);
                        request.AddJsonBody(new { rawtx = signedTransaction });

                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;

                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                            "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction
                            + "\r\n Result: " + content);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }
                if (userclaim.CoinShortName == "BCX")
                {
                    //https://explorer.bitcoininterest.io/api/
                    try
                    {
                        var client = new RestClient("https://bcx.info/insight-api/");
                        var request = new RestRequest("tx/send/", Method.POST);
                        request.AddJsonBody(new { rawtx = signedTransaction });

                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;

                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                            "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction
                            + "\r\n Result: " + content);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }
                // Broadcast via own nodes over RPC
                if (   userclaim.CoinShortName == "BBC" 
                    || userclaim.CoinShortName == "BTF"
                    || userclaim.CoinShortName == "BCL"
                    || userclaim.CoinShortName == "BCBC"
                    || userclaim.CoinShortName == "BTV" 
                    || userclaim.CoinShortName == "BCD"
                    || userclaim.CoinShortName == "UBTC"
                    || userclaim.CoinShortName == "BTW" 
                    || userclaim.CoinShortName == "BPA")
                {
                    try
                    {
                        string content = TransmitViaJSONRPC(ClaimId, db, userclaim, signedTransaction, userclaim.CoinShortName);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " RPC send failed", e.Message);
                    }
                }
                if (userclaim.CoinShortName == "BTCP")
                {
                    try
                    {
                        var client = new RestClient("https://explorer.btcprivate.org/api/");
                        var request = new RestRequest("tx/send/", Method.POST);
                        request.AddJsonBody(new { rawtx = signedTransaction });
                        //request.AddParameter("rawtx", signedTransaction);

                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                                                            //ViewBag.content = content;
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;
                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                            "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }
                else if (userclaim.CoinShortName == "BTG")
                {
                    try
                    {
                        var client = new RestClient("https://explorer.bitcoingold.org/insight-api/");
                        var request = new RestRequest("tx/send/", Method.POST);
                        request.AddJsonBody(new { rawtx = signedTransaction });
                        //request.AddParameter("rawtx", signedTransaction);

                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                                                            //ViewBag.content = content;
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;
                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                            "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }
                if (userclaim.CoinShortName == "BTX")
                {
                    try
                    {
                        var client = new RestClient("https://insight.bitcore.cc/api/");
                        var request = new RestRequest("tx/send", Method.POST);
                        //request.AddJsonBody(new { rawtx = signedTransaction });
                        request.AddParameter("rawtx", signedTransaction);
                        request.RequestFormat = DataFormat.Json;
                        //request.AddUrlSegment("rawtx", signedTransaction);
                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                                                            //ViewBag.content = content;
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;
                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                            "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }
                if (userclaim.CoinShortName == "BTV")
                {
                    //https://block.bitvote.one/tx/send   ps://block.bitvote.one/insight-api/
                    try
                    {
                        var client = new RestClient("https://block.bitvote.one/insight-api/");
                        var request = new RestRequest("tx/send/", Method.POST);
                        request.AddJsonBody(new { rawtx = signedTransaction });
                        //request.AddParameter("rawtx", signedTransaction);

                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                                                            //ViewBag.content = content;
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;
                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                            "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }

                }
                if (userclaim.CoinShortName == "BTP")
                {
                    try
                    {
                        var client = new RestClient("http://exp.btceasypay.com/insight-api/");
                        var request = new RestRequest("tx/send/", Method.POST);
                        request.AddJsonBody(new { rawtx = signedTransaction });
                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;
                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                            "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Json(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }

                // Transmit via Full Node
                try
                {
                    string url = "https://www.metabittrader.com/" + userclaim.CoinShortName + "/";
                    var client = new RestClient(url);
                    var request = new RestRequest("api/tx/", Method.POST);
                    request.AddJsonBody(new { Hex = Hex, ClaimId = ClaimId });
                    IRestResponse<BroadcastResponse> restResponse = client.Execute<BroadcastResponse>(request);
                    var content = restResponse.Data;
                    // Forward result
                    return Json(content);
                }
                catch (Exception e)
                {
                    MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                }
                response.Result = "Unknown error broadcasting.";
                response.Error = true;
                return Json(response);
            }
        }

        private static string TransmitViaJSONRPC(string ClaimId, CoinpanicContext db, CoinClaim userclaim, string signedTransaction, string coin)
        {
            GetConnectionDetails(coin, out string host, out int port, out string user, out string pass);
            var client = new RestClient("http://" + host + ":" + Convert.ToString(port));
            client.Authenticator = new HttpBasicAuthenticator(user, pass);
            var request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody(new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "sendrawtransaction",
                @params = new List<string>() { signedTransaction },
            });

            var restResponse = client.Execute(request);
            var content = restResponse.Content; // raw content as string
            userclaim.TransactionHash = content;
            userclaim.WasTransmitted = true;
            userclaim.SubmitDate = DateTime.Now;
            db.SaveChanges();
            MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue),
                "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
            return content;
        }

        public ActionResult ClaimError(string message, string claimId)
        {
            ViewBag.Title = "Claim Error";
            ViewBag.message = message;
            ViewBag.ClaimId = claimId;
            return View();
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


        public ActionResult InvalidCoin()
        {
            return View();
        }

        public ActionResult DownloadTransactionFile(string claimId)
        {
            using (CoinpanicContext db = new CoinpanicContext())
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
        }

        public ActionResult DownloadClaimDataFile(string claimId)
        {
            using (CoinpanicContext db = new CoinpanicContext())
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
        }
    }
}