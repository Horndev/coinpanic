using CoinpanicLib.Models;
using CoinpanicLib.NodeConnection;
using NBitcoin;
using NodeInterface.Database;
using NodeInterface.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace NodeInterface.Controllers
{
    public class HomeController : Controller
    {
        //private CoinpanicContext db = new CoinpanicContext();
        private readonly INodeService nodeService;

        public HomeController(INodeService ns)
        {
            nodeService = ns;
        }

        public ActionResult Connect(int? nid)
        {
            return RedirectToAction("Index");
        }

        public ActionResult Disconnect(int? nid)
        {
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? nid)
        {
            Debug.Write("Delete " + Convert.ToString(nid));
            if (nid == null)
            {
                return RedirectToAction("Index");
            }
            using (var db = new CoinpanicContext())
            {
                var sn = db.SeedNodes.Where(n => n.SeedNodeId == nid).FirstOrDefault();
                db.SeedNodes.Remove(sn);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult Index()
        {

            IndexViewModel vm = new IndexViewModel();
            using (var db = new CoinpanicContext())
            {
                var seednodes = db.SeedNodes.AsNoTracking().Where(n => n.Coin == nodeService.Coin).ToList();

                nodeService.ConnectNodes(seednodes.Select(n => new NodeDetails()
                {
                    coin = nodeService.Coin,
                    ip = n.IP,
                    port = n.Port,
                    use = n.Enabled,
                }).ToList());

                vm.Peers = seednodes.AsEnumerable().Select(n => new PeerModel()
                {
                    Id = n.SeedNodeId,
                    IP = n.IP,
                    port = n.Port,
                    Label = n.Label,
                    IsConnected = n.Enabled && (nodeService.TryGetNode(n.IP, n.Port) != null),
                    uptime = n.Enabled && (nodeService.TryGetNode(n.IP, n.Port) != null) ? ((nodeService.TryGetNode(n.IP, n.Port).State == NBitcoin.Protocol.NodeState.HandShaked || nodeService.TryGetNode(n.IP, n.Port).State == NBitcoin.Protocol.NodeState.Connected) ? nodeService.TryGetNode(n.IP, n.Port).Peer.Ago.ToString() : "") : "",
                    status = n.Enabled == false ? "Disabled" : (nodeService.TryGetNode(n.IP, n.Port) != null ? nodeService.TryGetNode(n.IP, n.Port).State.ToString() : ""),
                }).ToList();
                
            }
            ViewBag.Title = "Coinpanic Node Interface";
            ViewBag.Coin = nodeService.Coin;
            ViewBag.NumConnectedPeers = nodeService.NumConnectedPeers;

            return View(vm);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nid">Node Id</param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult EnableDisable(int? nid)
        {
            using (var db = new CoinpanicContext())
            {
                var sn = db.SeedNodes.FirstOrDefault(n => n.SeedNodeId == nid);
                sn.Enabled = !sn.Enabled;   //toggle.
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult AddNode(string label, string ip, int? port)
        {
            if (port != null)
            {
                SeedNode newNode = new SeedNode()
                {
                    Coin = nodeService.Coin,
                    IP = ip,
                    Label = label,
                    Port = Convert.ToInt32(port),
                    Enabled = true,
                };
                using (var db = new CoinpanicContext())
                {
                    db.SeedNodes.Add(newNode);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult RemoveNode()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Broadcast(IndexViewModel model)
        {
            try
            {
                // Verification 
                if (ModelState.IsValid)
                {
                    // If we don't have any connections, try to open them.
                    if (nodeService.NumConnectedPeers < 1)
                    {
                        using (var db = new CoinpanicContext())
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
                    }

                    var tx = model.Broadcast.Hex;
                    Transaction t = null;
                    try
                    {
                        t = Transaction.Parse(tx.Trim().Replace(" ", ""));
                    }
                    catch (Exception e)
                    {
                        //catch bad transactions
                        return this.Json(new
                        {
                            EnableSuccess = true,
                            SuccessTitle = "Error",
                            SuccessMsg = e.Message,
                        });
                        //MonitoringService.SendMessage("Invalid tx " + userclaim.CoinShortName + " submitted " + Convert.ToString(userclaim.TotalValue), "Claim broadcast: https://www.coinpanic.com/Claim/ClaimConfirm?claimId=" + claimId + " " + " for " + userclaim.CoinShortName + "\r\n " + signedTransaction);
                        //return RedirectToAction("ClaimError", new { message = e.Message + ". Unable to parse signed transaction: \r\n" + tx, claimId = claimId });
                    }
                    string txid = t.GetHash().ToString();
                    var res = nodeService.BroadcastTransaction(t, false);
                    //Thread.Sleep(5000); //Simulate delay
                    // Info.  
                    return this.Json(new
                    {
                        EnableSuccess = true,
                        SuccessTitle = t.GetHash().ToString(),
                        SuccessMsg = res.Result,
                    });
                }
            }
            catch (Exception ex)
            {
                // Info  
                Console.Write(ex);
            }
            // Info  
            return this.Json(new
            {
                EnableError = true,
                ErrorTitle = "Error",
                ErrorMsg = "Something goes wrong, please try again later"
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CheckTxStatus(string txid)
        {
            return this.Json(new
            {
                EnableSuccess = true,
                SuccessMsg = "Checked Tx Status"
            });
        }
    }
}
