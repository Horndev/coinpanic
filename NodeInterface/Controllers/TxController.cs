﻿using CoinpanicLib.Models;
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

namespace NodeInterface.Controllers
{
    /// <summary>
    /// Transactions Controller
    /// </summary>
    [RoutePrefix("api/tx")]
    [EnableCors(origins: "https://www.coinpanic.com", headers: "*", methods: "*", SupportsCredentials = true)]
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
                userclaim.SignedTX = signedTransaction.Trim().Replace(" ", "");
                db.SaveChanges();

                BroadcastResponse response = new BroadcastResponse()
                {
                    Error = false,
                    Result = "Transaction successfully broadcast.",
                    Txid = "",
                };
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

                    var tx = b.Hex;
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