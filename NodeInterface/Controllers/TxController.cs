using CoinpanicLib.Models;
using CoinpanicLib.NodeConnection;
using CoinpanicLib.NodeConnection.Api;
using NBitcoin;
using NodeInterface.Database;
using NodeInterface.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using static CoinpanicLib.Services.MailingService;
using RestSharp;

namespace NodeInterface.Controllers
{
    /// <summary>
    /// Transactions Controller
    /// </summary>
    [RoutePrefix("api/tx")]
    [EnableCors(origins: "https://www.coinpanic.com,https://coinpanic.com", headers: "*", methods: "*", SupportsCredentials = true)]
    public class TxController : ApiController
    {
        //private CoinpanicContext db = new CoinpanicContext();
        private readonly INodeService nodeService;

        public TxController(INodeService ns)
        {
            nodeService = ns;
            Debug.Print("Tx Controller created.");
        }

        /// <summary>
        /// Transmits a transaction to the network.
        /// </summary>
        /// <param name="hex">The raw transaction in hexadecimal format.</param>
        [Route("")]
        [HttpPost]
        public IHttpActionResult Post([FromBody] BroadcastModel b)
        {
            using (CoinpanicContext db = new CoinpanicContext())
            {
                //MonitoringService.SendMessage("Received tx POST " + b.ClaimId, "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId );
                CoinClaim userclaim = db.Claims.Where(c => c.ClaimId == b.ClaimId).FirstOrDefault();
                
                if (userclaim == null)
                {
                    userclaim = new CoinClaim();
                }

                //Clean up the signed transaction Hex
                string signedTransaction = b.Hex;
                signedTransaction = signedTransaction.Replace("\n", String.Empty);
                signedTransaction = signedTransaction.Replace("\r", String.Empty);
                signedTransaction = signedTransaction.Replace("\t", String.Empty);
                signedTransaction = signedTransaction.Trim().Replace(" ", "");
                userclaim.SignedTX = signedTransaction;
                userclaim.SubmitDate = DateTime.Now;
                db.SaveChanges();

                BroadcastResponse response = new BroadcastResponse()
                {
                    Error = false,
                    Result = "Transaction successfully broadcast.",
                    Txid = "",
                };
                var tx = signedTransaction;

                if (tx == "")
                {
                    response.Result = "Error: No signed transaction provided.";
                    MonitoringService.SendMessage("Empty tx " + userclaim.CoinShortName + " submitted.", "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId);
                    return Ok(response);
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
                    MonitoringService.SendMessage("Invalid tx " + userclaim.CoinShortName + " submitted " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                    return Ok(response);
                }

                //BCX submit
                if (nodeService.Coin == "BCX")
                {

                }
                if (nodeService.Coin == "BTCP")
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
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Ok(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage("B2X explorer send failed", e.Message);
                    }
                }
                if (nodeService.Coin == "BTG")
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
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Ok(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }
                if (nodeService.Coin == "BTX")
                {
                    try
                    {
                        var client = new RestClient("http://insight.bitcore.cc/api/");
                        var request = new RestRequest("tx/send", Method.POST);
                        request.AddJsonBody(new { rawtx = signedTransaction });
                        //request.AddParameter("rawtx", signedTransaction);

                        IRestResponse restResponse = client.Execute(request);
                        var content = restResponse.Content; // raw content as string
                                                            //ViewBag.content = content;
                        userclaim.TransactionHash = content;
                        userclaim.WasTransmitted = true;
                        userclaim.SubmitDate = DateTime.Now;
                        db.SaveChanges();
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Ok(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage(userclaim.CoinShortName + " explorer send failed", e.Message);
                    }
                }
                if (nodeService.Coin == "B2X")
                {
                    try
                    {
                        var client = new RestClient("https://explorer.b2x-segwit.io/b2x-insight-api/");
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
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Ok(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage("B2X explorer send failed", e.Message);
                    }
                }
                if (nodeService.Coin == "BTV")
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
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Ok(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage("BTV explorer send failed", e.Message);
                    }
                    
                }
                if (nodeService.Coin == "BTP")
                {
                    try
                    {
                        var client = new RestClient("http://exp.btceasypay.com/insight-api/");
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
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting via explorer " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        response.Result = content;
                        return Ok(response);
                    }
                    catch (Exception e)
                    {
                        MonitoringService.SendMessage("BTV explorer send failed", e.Message);
                    }
                    //MonitoringService.SendMessage("New " + userclaim.CoinShortName + " begin broadcasting via explorer " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);

                    //var bitpieClient = new RestClient
                    //{
                    //    BaseUrl = new Uri("https://bitpie.getcai.com/api/v1/")
                    //};
                    //var txRequest = new RestRequest("/btp/broadcast", Method.POST);

                    //string data = "{\"raw_tx\": \""+ userclaim.SignedTX + "\"}";
                    //txRequest.AddParameter("application/json; charset=utf-8", data, ParameterType.RequestBody);
                    //txRequest.RequestFormat = DataFormat.Json;
                    //try
                    //{
                    //    var txresponse = bitpieClient.Execute(txRequest);
                    //    if (txresponse.IsSuccessful)
                    //    {
                    //        if (txresponse.Content == "{\"result\": 0, \"error\": \"\"}")
                    //        {
                    //            response.Result = "Transaction was broadcast.  Network reported unknown error.  Double check signatures and ensure coins not already claimed.";
                    //        }
                    //        else if (txresponse.Content == "{\"result\": 0, \"error\": \"broadcast error\"}")
                    //        {
                    //            response.Result = "Transaction successfully broadcast.  No known errors identified.";
                    //        }
                    //        else
                    //        { 
                    //            response.Result = "Transaction successfully broadcast.  Result code: " + txresponse.Content;
                    //        }
                    //        response.Txid = t.GetHash().ToString();
                    //    }
                    //    else
                    //    {
                    //        response.Result = "Error sending transactoin.  Node service unavailable.";
                    //    }
                    //    Debug.Print(txresponse.StatusDescription);
                    //    MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcast via explorer " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                    //}
                    //catch (Exception e)
                    //{
                    //    MonitoringService.SendMessage("Tx server error", e.Message);
                    //    response.Result = "Transmission Error";
                    //    return InternalServerError();
                    //}
                    //return Ok(response);
                }

                //Regular nodes
                try
                {
                    // If we don't have any connections, try to open them.
                    if (nodeService.NumConnectedPeers < 1)
                    {
                        var seednodes = db.SeedNodes.Where(n => n.Coin == nodeService.Coin);
                        nodeService.ConnectNodes(seednodes.Select(n => new NodeDetails()
                        {
                            coin = nodeService.Coin,
                            ip = n.IP,
                            port = n.Port,
                            use = n.Enabled,
                        }).ToList());
                    }

                    string txid = t.GetHash().ToString();
                    response.Txid = txid;
                    userclaim.TransactionHash = txid;
                    db.SaveChanges();

                    var res = nodeService.BroadcastTransaction(t, false);
                    if (res.IsError)
                    {
                        userclaim.WasTransmitted = false;
                        userclaim.SignedTX = signedTransaction;
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " error broadcasting " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n txid: " + txid + "\r\nResult: " + res.Result);
                    }
                    else
                    {
                        userclaim.TransactionHash = txid;
                        userclaim.WasTransmitted = true;
                        userclaim.SignedTX = signedTransaction;
                        MonitoringService.SendMessage("New " + userclaim.CoinShortName + " broadcasting " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + b.ClaimId + " " + " for " + userclaim.CoinShortName + "\r\n txid: " + txid + "\r\nResult: " + res.Result);
                    }
                    response.Result = res.Result;
                    db.SaveChanges();
                    return Ok(response);
                }
                catch (Exception e)
                {
                    MonitoringService.SendMessage("Tx server error", e.Message);
                    return InternalServerError();
                }
            }
        }

        /// <summary>
        /// Gets the status of a transaction
        /// </summary>
        /// <param name="txid">The transaction id.</param>
        /// <returns>Returns information about the requested transaction, or null if unknown.</returns>
        [Route("{txid}")]
        [ResponseType(typeof(TxInfo))]
        public TxInfo Get(string txid)
        {
            nodeService.Test();
            nodeService.Val = txid;
            return new TxInfo();
        }
    }
}
