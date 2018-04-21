using coinpanic_airdrop.Database;
using coinpanic_airdrop.Models;
using coinpanic_airdrop.Services;
using LightningLib.lndrpc;
using Microsoft.AspNet.SignalR;
using QRCoder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static CoinpanicLib.Services.MailingService;

namespace coinpanic_airdrop.Controllers
{
    public class LightningController : Controller
    {
        private static ConcurrentDictionary<Guid, TransactionListener> lndclients = new ConcurrentDictionary<Guid, TransactionListener>();

        private static bool usingTestnet = true;

        public ActionResult CommunityJar()
        {
            usingTestnet = GetUseTestnet();

            var lndClient = new LndRpcClient(
                host: System.Configuration.ConfigurationManager.AppSettings[usingTestnet ? "LnTestnetHost" : "LnMainnetHost"],
                macaroonAdmin: System.Configuration.ConfigurationManager.AppSettings[usingTestnet ? "LnTestnetMacaroonAdmin" : "LnMainnetMacaroonAdmin"],
                macaroonRead: System.Configuration.ConfigurationManager.AppSettings[usingTestnet ? "LnTestnetMacaroonRead" : "LnMainnetMacaroonRead"]);

            var info = lndClient.GetInfo();
            ViewBag.URI = info.uris.First();

            string userId = "";
            //Check if user is returning
            if (HttpContext.Request.Cookies["CoinpanicCommunityJarUser"] != null)
            {
                var cookie = HttpContext.Request.Cookies.Get("CoinpanicCommunityJarUser");
                cookie.Expires = DateTime.Now.AddDays(7);   //update
                HttpContext.Response.Cookies.Remove("CoinpanicCommunityJarUser");
                HttpContext.Response.SetCookie(cookie);
                userId = cookie.Value;
            }
            else
            {
                HttpCookie cookie = new HttpCookie("CoinpanicCommunityJarUser");
                cookie.Value = Guid.NewGuid().ToString();
                cookie.Expires = DateTime.Now.AddDays(7);
                HttpContext.Response.Cookies.Remove("CoinpanicCommunityJarUser");
                HttpContext.Response.SetCookie(cookie);
                userId = cookie.Value;
            }

            LnCJTransactions latestTx = new LnCJTransactions();

            using (CoinpanicContext db = new CoinpanicContext())
            {
                var jar = db.LnCommunityJars.Where(j => j.IsTestnet == usingTestnet).First();
                ViewBag.Balance = jar.Balance;

                latestTx.Transactions = jar.Transactions.OrderByDescending(t => t.TimestampSettled).Take(20).Select(t => new LnCJTransaction()
                {
                    Timestamp = t.TimestampSettled == null ? DateTime.UtcNow : (DateTime)t.TimestampSettled,
                    Amount = t.Value,
                    Memo = t.Memo,
                    Type = t.IsDeposit ? "Deposit" : "Withdrawal",
                }).ToList();
                latestTx.Balance = jar.Balance;
            }

            return View(latestTx);
        }

        public ActionResult WebWallet()
        {
            return View();
        }

        public ActionResult GetQR(string qr)
        {
            if (qr is null || qr == "")
                qr = "test";
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qr, QRCodeGenerator.ECCLevel.L);//, forceUtf8: true);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            MemoryStream ms = new MemoryStream();
            qrCodeImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return File(ms.ToArray(), "image/png");
        }

        [HttpPost]
        public ActionResult GetJarBalances()
        {
            bool useTestnet = GetUseTestnet();
            string ip = Request.UserHostAddress;
            string balance;
            string userDeposits = "0";
            string userWithdraws = "0";
            using (CoinpanicContext db = new CoinpanicContext())
            {
                var jar = db.LnCommunityJars.Where(j => j.IsTestnet == useTestnet).First();
                balance = Convert.ToString(jar.Balance);
                var user = db.LnCommunityJarUsers.Where(u => u.UserIP == ip).ToList();
            }
            return Json(new { Balance = balance, Deposits = userDeposits, Withdraws=userWithdraws });
        }

        //Used for rate limiting double withdraws
        static ConcurrentDictionary<string, DateTime> WithdrawRequests = new ConcurrentDictionary<string, DateTime>();

        [HttpPost]
        public ActionResult SubmitPaymentRequest(string request)
        {
            int minwithdraw = 150;
            string ip = Request.UserHostAddress;
            //if (ip == "99.43.41.3")
            //{
            //    return Json(new { Result = "Error: Bad behaviour detected from your IP" });
            //}

            bool useTestnet = GetUseTestnet();
            var lndClient = new LndRpcClient(
                host: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetHost" : "LnMainnetHost"],
                macaroonAdmin: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetMacaroonAdmin" : "LnMainnetMacaroonAdmin"],
                macaroonRead: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetMacaroonRead" : "LnMainnetMacaroonRead"]);

            try
            {
                string userId = "";
                // Check if user is returning
                if (HttpContext.Request.Cookies["CoinpanicCommunityJarUser"] != null)
                {
                    // Returning user - look up in database
                    var cookie = HttpContext.Request.Cookies.Get("CoinpanicCommunityJarUser");
                    cookie.Expires = DateTime.Now.AddDays(7);   // update expiry
                    HttpContext.Response.Cookies.Remove("CoinpanicCommunityJarUser");
                    HttpContext.Response.SetCookie(cookie);
                    userId = cookie.Value;
                }
                else
                {
                    // Create new cookie
                    HttpCookie cookie = new HttpCookie("CoinpanicCommunityJarUser");
                    cookie.Value = Guid.NewGuid().ToString();
                    cookie.Expires = DateTime.Now.AddDays(7);
                    HttpContext.Response.Cookies.Remove("CoinpanicCommunityJarUser");
                    HttpContext.Response.SetCookie(cookie);
                    userId = cookie.Value;
                }

                //check if payment request is ok
                //Check if already paid

                var decoded = lndClient.DecodePayment(request);
                
                if (decoded.destination == null)
                {
                    return Json(new { Result = "Error decoding invoice." });
                }
                if (Convert.ToInt64(decoded.num_satoshis) > minwithdraw)
                {
                    return Json(new { Result = "Requested amount is greater than maximum allowed." });
                }

                // Check that there are funds in the Jar
                Int64 balance;
                LnCommunityJar jar;
                using (CoinpanicContext db = new CoinpanicContext())
                {
                    jar = db.LnCommunityJars.Where(j => j.IsTestnet == useTestnet).AsNoTracking().First();
                    balance = jar.Balance;
                    
                }
                if (Convert.ToInt64(decoded.num_satoshis) > balance)
                {
                    return Json(new { Result = "Requested amount is greater than the available balance." });
                }

                //Check rate limits
                LnCJUser user;
                using (CoinpanicContext db = new CoinpanicContext())
                {
                    //Get user
                    user = GetUserFromDb(userId, db, jar, ip);

                    //check if new user
                    DateTime? LastWithdraw = user.TimesampLastWithdraw;
                    //LastWithdraw = db.LnTransactions.Where(tx => tx.IsDeposit == false && tx.IsSettled == true && tx.UserId == user.LnCJUserId).OrderBy(tx => tx.TimestampCreated).AsNoTracking().First().TimestampCreated;
                    if (user.NumWithdraws == 0 && user.NumDeposits == 0)
                    {
                        //check ip (if someone is not using cookies to rob the site)
                        var userIPs = db.LnCommunityJarUsers.Where(u => u.UserIP == ip).ToList();
                        if (userIPs.Count > 1)
                        {
                            //most recent withdraw
                            LastWithdraw = userIPs.Max(u => u.TimesampLastWithdraw);
                        }
                    }

                    if (user.TotalDeposited - user.TotalWithdrawn < minwithdraw)
                    {
                        //check for time rate limiting
                        if (DateTime.UtcNow - LastWithdraw < TimeSpan.FromHours(1))
                        {
                            return Json(new { Result = "You must wait another " + ((user.TimesampLastWithdraw+TimeSpan.FromHours(1))-DateTime.UtcNow).Value.TotalMinutes.ToString("0.0") + " minutes before withdrawing again, or make a deposit first." });
                        }
                    }

                    //Check if already paid
                    if (db.LnTransactions.Where(tx => tx.PaymentRequest == request && tx.IsSettled).Count() > 0)
                    {
                        return Json(new { Result = "Invoice has already been paid." });
                    }
                }

                SendPaymentResponse paymentresult;
                //all ok - make the payment
                if (WithdrawRequests.TryAdd(request, DateTime.UtcNow))
                {
                    paymentresult = lndClient.PayInvoice(request);
                }
                else
                {
                    //double request
                    Thread.Sleep(1000);

                    //Check if paid
                    using (CoinpanicContext db = new CoinpanicContext())
                    {
                        var txs = db.LnTransactions.Where(t => t.PaymentRequest == request && t.IsSettled).OrderByDescending(t => t.TimestampSettled).AsNoTracking();
                        if (txs.Count() > 0)
                        {
                            //var tx = txs.First();
                            WithdrawRequests.TryRemove(request, out DateTime reqInitTimeA);
                            return Json(new { Result = "success", Fees = "0" });
                        }
                        
                        return Json(new { Result = "Please click only once.  Payment already in processing." });
                    }
                }
                WithdrawRequests.TryRemove(request, out DateTime reqInitTime);
                
                
                if (paymentresult.payment_error != null)
                {
                    return Json(new { Result = "Payment Error: " + paymentresult.payment_error });
                }

                var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
                using (CoinpanicContext db = new CoinpanicContext())
                {
                    user = GetUserFromDb(userId, db, jar, ip);
                    user.NumWithdraws += 1;
                    user.TotalWithdrawn += Convert.ToInt64(decoded.num_satoshis);
                    user.TimesampLastWithdraw = DateTime.UtcNow;

                    //check if unsettled transaction exists?

                    //insert transaction
                    LnTransaction t = new LnTransaction()
                    {
                        UserId = user.LnCJUserId,
                        IsSettled = true,
                        Memo = "Withdraw",
                        Value = Convert.ToInt64(decoded.num_satoshis),
                        IsTestnet = GetUseTestnet(),
                        HashStr = decoded.payment_hash,
                        IsDeposit = false,
                        TimestampSettled = DateTime.UtcNow,
                        TimestampCreated = DateTime.UtcNow, //can't kbnow
                        PaymentRequest = request,
                    };
                    db.LnTransactions.Add(t);

                    jar = db.LnCommunityJars.Where(j => j.IsTestnet == useTestnet).First();
                    jar.Balance -= Convert.ToInt64(decoded.num_satoshis);
                    jar.Balance -= paymentresult.payment_route.total_fees != null ? Convert.ToInt64(paymentresult.payment_route.total_fees) : 0;

                    jar.Transactions.Add(t);
                    db.SaveChanges();

                    var newT = new LnCJTransaction()
                    {
                        Timestamp = t.TimestampSettled == null ? DateTime.UtcNow : (DateTime)t.TimestampSettled,
                        Amount = t.Value,
                        Memo = t.Memo,
                        Type = t.IsDeposit ? "Deposit" : "Withdrawal",
                    };

                    context.Clients.All.NotifyNewTransaction(newT);
                }

                return Json(new { Result = "success", Fees = (paymentresult.payment_route.total_fees == null ? "0" : paymentresult.payment_route.total_fees) });
            }
            catch (Exception e)
            {
                return Json(new { Result = "Error decoding request." });
            }

            return Json(new { Result = "success" });
        }

        [HttpPost]
        public ActionResult GetJarDepositInvoice(string amount)
        {
            string ip = Request.UserHostAddress;
            string memo = "Coinpanic Community Jar";

            bool useTestnet = GetUseTestnet();
            var lndClient = new LndRpcClient(
                    host: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetHost" : "LnMainnetHost"],
                    macaroonAdmin: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetMacaroonAdmin" : "LnMainnetMacaroonAdmin"],
                    macaroonRead: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetMacaroonRead" : "LnMainnetMacaroonRead"],
                    macaroonInvoice: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetMacaroonInvoice" : "LnMainnetMacaroonInvoice"]);

            var inv = lndClient.AddInvoice(Convert.ToInt64(amount), memo:memo, expiry:"432000");

            LnRequestInvoiceResponse resp = new LnRequestInvoiceResponse()
            {
                Invoice = inv.payment_request,
                Result = "success",
            };

            string userId = "";
            //Check if user is returning
            if (HttpContext.Request.Cookies["CoinpanicCommunityJarUser"] != null)
            {
                var cookie = HttpContext.Request.Cookies.Get("CoinpanicCommunityJarUser");
                cookie.Expires = DateTime.Now.AddDays(7);   //update
                HttpContext.Response.Cookies.Remove("CoinpanicCommunityJarUser");
                HttpContext.Response.SetCookie(cookie);
                userId = cookie.Value;
            }
            else
            {
                HttpCookie cookie = new HttpCookie("CoinpanicCommunityJarUser");
                cookie.Value = Guid.NewGuid().ToString();
                cookie.Expires = DateTime.Now.AddDays(7);
                HttpContext.Response.Cookies.Remove("CoinpanicCommunityJarUser");
                HttpContext.Response.SetCookie(cookie);
                userId = cookie.Value;
            }

            //Create transaction record (not settled)

            using (CoinpanicContext db = new CoinpanicContext())
            {
                var jar = db.LnCommunityJars.Where(j => j.IsTestnet == useTestnet).First();

                //is this a previous user?
                LnCJUser user;
                user = GetUserFromDb(userId, db, jar, ip);

                //create a new transaction
                LnTransaction t = new LnTransaction()
                {
                    UserId = user.LnCJUserId,
                    IsSettled = false,
                    Memo = memo,
                    Value = Convert.ToInt64(amount),
                    IsTestnet = GetUseTestnet(),
                    HashStr = inv.r_hash,
                    IsDeposit = true,
                    //TimestampSettled = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc) + TimeSpan.FromSeconds(Convert.ToInt64(invoice.settle_date)),
                    TimestampCreated = DateTime.Now,
                    PaymentRequest = inv.payment_request,
                };
                db.LnTransactions.Add(t);
                db.SaveChanges();
            }

            var listener = lndClient.GetListener();
            lndclients.TryAdd(listener.ListenerId, listener);           //keep alive while we wait for payment
            listener.InvoicePaid += NotifyClientsInvoicePaid;     //handle payment message
            listener.StreamLost += OnListenerLost;                  //stream lost
            var a = new Task(() => listener.Start());                   //listen for payment
            a.Start();

            return Json(resp);
        }

        private static LnCJUser GetUserFromDb(string userId, CoinpanicContext db, LnCommunityJar jar, string ip)
        {
            LnCJUser user;
            var users = db.LnCommunityJarUsers.Where(u => u.LnCJUserId == userId).ToList();

            if (users.Count == 0)
            {
                // new user
                user = new LnCJUser()
                {
                    LnCJUserId = userId,
                    JarId = jar.JarId,
                    UserIP = ip,
                    NumDeposits = 0,
                    NumWithdraws = 0,
                    TotalDeposited = 0,
                    TotalWithdrawn = 0,
                    TimesampLastDeposit = DateTime.UtcNow - TimeSpan.FromDays(1),
                    TimesampLastWithdraw = DateTime.UtcNow - TimeSpan.FromDays(1),
                };
                db.LnCommunityJarUsers.Add(user);
            }
            else if (users.Count > 1)
            {
                // error
                throw new Exception("User database error: multiple users with same id.");
            }
            else
            {
                user = users.First();
            }

            // Ensure copy in usersDB
            if (db.LnCommunityUsers.Where(u => u.UserId == user.LnCJUserId).Count() < 1)
            {
                // need to add to db
                LnUser newu = new LnUser()
                {
                    UserId = user.LnCJUserId,
                    Balance = user.TotalDeposited - user.TotalWithdrawn,
                };
                db.LnCommunityUsers.Add(newu);
            }

            db.SaveChanges();

            return user;
        }

        private static void OnListenerLost(TransactionListener l)
        {
            lndclients.TryRemove(l.ListenerId, out TransactionListener oldListener);
        }

        private static void NotifyClientsInvoicePaid(Invoice invoice)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            bool isTesnet = GetUseTestnet();

            //Save in db
            using (CoinpanicContext db = new CoinpanicContext())
            {
                var jar = db.LnCommunityJars.Where(j => j.IsTestnet == isTesnet).First();

                //check if unsettled transaction exists
                var tx = db.LnTransactions.Where(tr => tr.PaymentRequest == invoice.payment_request).ToList();

                LnTransaction t;
                if (tx.Count > 0)
                {
                    t = tx.First();
                    t.TimestampSettled = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc) + TimeSpan.FromSeconds(Convert.ToInt64(invoice.settle_date));
                    t.IsSettled = true;
                }
                else
                {
                    //insert transaction
                    t = new LnTransaction()
                    {
                        IsSettled = invoice.settled,
                        Memo = invoice.memo,
                        Value = Convert.ToInt64(invoice.value),
                        IsTestnet = GetUseTestnet(),
                        HashStr = invoice.r_hash,
                        IsDeposit = true,
                        TimestampSettled = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc) + TimeSpan.FromSeconds(Convert.ToInt64(invoice.settle_date)),
                        TimestampCreated = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc) + TimeSpan.FromSeconds(Convert.ToInt64(invoice.creation_date)),
                        PaymentRequest = invoice.payment_request,
                        UserId = Guid.NewGuid().ToString(),
                    };
                    db.LnTransactions.Add(t);
                }

                var userId = t.UserId;
                var user = GetUserFromDb(userId, db, jar, 
                    ip: ""  // only used when creating a new user, so set blank for this.
                    );

                user.TotalDeposited += Convert.ToInt64(invoice.value);
                user.NumDeposits += 1;
                user.TimesampLastDeposit = DateTime.UtcNow;

                t.IsSettled = true;

                jar.Balance += Convert.ToInt64(invoice.value);
                jar.Transactions.Add(t);
                db.SaveChanges();

                // Notify Web clients - this is shown to user
                var newT = new LnCJTransaction()
                {
                    Timestamp = t.TimestampSettled == null ? DateTime.UtcNow : (DateTime)t.TimestampSettled,
                    Amount = t.Value,
                    Memo = t.Memo,
                    Type = t.IsDeposit ? "Deposit" : "Withdrawal",
                };

                context.Clients.All.NotifyNewTransaction(newT);
                context.Clients.All.NotifyInvoicePaid(invoice.payment_request);
            }
        }

        private static bool GetUseTestnet()
        {
            return System.Configuration.ConfigurationManager.AppSettings["LnUseTestnet"] == "true";
        }

        // GET: Lightning
        public ActionResult Index()
        {
            try
            {
                bool useTestnet = GetUseTestnet();

                var lndClient = new LndRpcClient(
                    host: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetHost" : "LnMainnetHost"],
                    macaroonAdmin: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetMacaroonAdmin" : "LnMainnetMacaroonAdmin"],
                    macaroonRead: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetMacaroonRead" : "LnMainnetMacaroonRead"]);

                var info = lndClient.GetInfo();
                var channels = lndClient.GetChannels();

                var LnStatusViewModel = new LnStatusViewModel();
                LnStatusViewModel.channels = new List<LnChannelInfoModel>();

                using (CoinpanicContext db = new CoinpanicContext())
                {
                    LnNode myNode = GetOrCreateNode(lndClient, info.identity_pubkey, db);

                    //Check each channel
                    foreach (var c in channels.channels)
                    {
                        LnChannelInfoModel channelViewModel = new LnChannelInfoModel();

                        // Check if this is a new channel
                        if (myNode.Channels.Where(ch => ch.ChannelId == c.chan_id).Count() < 1)
                        {
                            LnChannel thisChannel = GetOrCreateChannel(lndClient, db, c);
                            
                            if (!myNode.Channels.Contains(thisChannel))
                            {
                                myNode.Channels.Add(thisChannel);
                                db.SaveChanges();
                            }
                        }

                        // Check if there is a history for the channel
                        //List<LnChannelConnectionPoints> chanHist = GetChanHist(lndClient, db, c);
                        DateTime cutoff = DateTime.UtcNow - TimeSpan.FromDays(30);
                        Int64 otherchanid = Convert.ToInt64(c.chan_id);
                        channelViewModel.History = db.LnChannelHistory
                            .Where(ch => ch.ChanId == otherchanid)
                            .Where(ch => ch.Timestamp > cutoff)
                            .OrderByDescending(ch => ch.Timestamp)
                            .Include("RemoteNode")
                            .Take(30)
                            .AsNoTracking()
                            .ToList();

                        LnChannelConnectionPoints prevChanHist;
                        if (channelViewModel.History.Count() > 0)
                        {
                            prevChanHist = channelViewModel.History.First();
                        }
                        else
                        {
                            prevChanHist = new LnChannelConnectionPoints()
                            {
                                Timestamp = DateTime.UtcNow,
                            };
                        }
                        
                        // check for changes
                        if (prevChanHist.IsConnected != c.active
                            || prevChanHist.LocalBalance != Convert.ToInt64(c.local_balance)
                            || prevChanHist.RemoteBalance != Convert.ToInt64(c.remote_balance)
                            || DateTime.UtcNow - prevChanHist.Timestamp > TimeSpan.FromHours(6))
                        {
                            // update
                            LnNode remoteNode = GetOrCreateNode(lndClient, c.remote_pubkey, db);
                            LnChannelConnectionPoints newChanHist = new LnChannelConnectionPoints()
                            {
                                IsConnected = c.active,
                                LocalBalance = Convert.ToInt64(c.local_balance),
                                RemoteBalance = Convert.ToInt64(c.remote_balance),
                                Timestamp = DateTime.UtcNow,
                                RemoteNode = remoteNode,
                                ChanId = Convert.ToInt64(c.chan_id),
                            };
                            prevChanHist.RemoteNode = remoteNode;
                            db.LnChannelHistory.Add(newChanHist);
                            db.SaveChanges();
                        }
                        if (c.remote_balance is null)
                        {
                            c.remote_balance = "0";
                        }
                        if (c.local_balance is null)
                        {
                            c.local_balance = "0";
                        }
                        channelViewModel.ChanInfo = c;
                        channelViewModel.RemoteNode = prevChanHist.RemoteNode;
                        LnStatusViewModel.channels.Add(channelViewModel);
                    }
                }

                ViewBag.URI = info.uris.First();
                ViewBag.NumChannelsActive = info.num_active_channels;
                ViewBag.Alias = info.alias;
                ViewBag.NumChannels = channels.channels.Count;
                ViewBag.Capacity = Convert.ToDouble(channels.channels.Sum(c => Convert.ToInt64(c.capacity))) / 100000000.0;

                //Total capacity on remote nodes
                //Total capacity on local node
                ViewBag.LocalCapacity = Convert.ToDouble(channels.channels.Sum(n => Convert.ToInt64(n.local_balance))) / 100000000.0;
                ViewBag.RemoteCapacity = Convert.ToDouble(channels.channels.Sum(n => Convert.ToInt64(n.remote_balance))) / 100000000.0;

                ViewBag.ActiveCapacity = Convert.ToDouble(channels.channels.Where(c=>c.active).Sum(c => Convert.ToInt64(c.capacity))) / 100000000.0;
                ViewBag.ActiveLocalCapacity = Convert.ToDouble(channels.channels.Where(c => c.active).Sum(n => Convert.ToInt64(n.local_balance))) / 100000000.0;
                ViewBag.ActiveRemoteCapacity = Convert.ToDouble(channels.channels.Where(c => c.active).Sum(n => Convert.ToInt64(n.remote_balance))) / 100000000.0;

                try
                {
                    var xfers = lndClient.GetForwardingEvents();

                    //Total amount transferred
                    ViewBag.TotalValueXfer = Convert.ToDouble(xfers.forwarding_events.Sum(f => Convert.ToInt64(f.amt_out))) / 100000000.0;
                    ViewBag.NumXfer = xfers.forwarding_events.Count;
                    ViewBag.TotalFees = (Convert.ToDouble(xfers.forwarding_events.Sum(f => Convert.ToInt64(f.fee))) / 100000000.0).ToString("0.00000000");
                }
                catch (Exception e)
                {
                    ViewBag.TotalValueXfer = "Unknown";
                    ViewBag.NumXfer = "Unknown";
                    ViewBag.TotalFees = "Unknown";
                    MonitoringService.SendMessage("Lightning Error", e.Message);
                }

                return View(LnStatusViewModel);
            }
            catch (Exception e)
            {
                return RedirectToAction("NodeError", new { message = "Error communicating with Lightning Node"});
            }
        }

        private static List<LnChannelConnectionPoints> GetChanHist(LndRpcClient lndClient, CoinpanicContext db, Channel c)
        {
            List<LnChannelConnectionPoints> chanHist;
            Int64 otherchanId = Convert.ToInt64(c.chan_id);
            var ch = db.LnChannelHistory.Where(h => h.ChanId == otherchanId);

            if (ch.Count() > 0)
            {
                // already known - check status
                chanHist = ch.OrderByDescending(h => h.Timestamp).AsNoTracking().ToList();
            }
            else
            {
                LnNode remoteNode = GetOrCreateNode(lndClient, c.remote_pubkey, db);
                // new channel history
                LnChannelConnectionPoints newChanHist = new LnChannelConnectionPoints()
                {
                    IsConnected = c.active,
                    LocalBalance = Convert.ToInt64(c.local_balance),
                    RemoteBalance = Convert.ToInt64(c.remote_balance),
                    Timestamp = DateTime.UtcNow,
                    RemoteNode = remoteNode,
                    ChanId = Convert.ToInt64(c.chan_id),
                };
                db.LnChannelHistory.Add(newChanHist);
                db.SaveChanges();
                chanHist = new List<LnChannelConnectionPoints>() { newChanHist };
            }

            return chanHist;
        }

        private static LnChannel GetOrCreateChannel(LndRpcClient lndClient, CoinpanicContext db, Channel c)
        {
            LnChannel thisChannel;
            var chanFind = db.LnChannels.Where(ch => ch.ChannelId == c.chan_id);
            if (chanFind.Count() < 1)
            {
                var chan = lndClient.GetChanInfo(c.chan_id);
                var Node1 = GetOrCreateNode(lndClient, chan.node1_pub, db);
                var Node2 = GetOrCreateNode(lndClient, chan.node2_pub, db);
                // not in database
                thisChannel = new LnChannel()
                {
                    Capacity = Convert.ToInt64(chan.capacity),
                    ChannelId = chan.channel_id,
                    ChanPoint = chan.chan_point,
                    Node1 = Node1,
                    Node2 = Node2,
                };
                db.SaveChanges();
            }
            else
            {
                thisChannel = chanFind.First();
            }
            return thisChannel;
        }

        private static LnNode GetOrCreateNode(LndRpcClient lndClient, string pubkey, CoinpanicContext db)
        {
            var nodeFind = db.LnNodes.Where(n => n.PubKey == pubkey).Include("Channels");
            LnNode theNode;
            if (nodeFind.Count() < 1)
            {
                // no record yet of node!
                var nodeInfo = lndClient.GetNodeInfo(pubkey);
                theNode = new LnNode()
                {
                    Alias = nodeInfo.node.alias,
                    Color = nodeInfo.node.color,
                    last_update = nodeInfo.node.last_update,
                    PubKey = nodeInfo.node.pub_key,
                };
                theNode.Channels = new HashSet<LnChannel>();
                db.LnNodes.Add(theNode);
                db.SaveChanges();
            }
            else
            {
                theNode = nodeFind.First();
            }

            return theNode;
        }

        public ActionResult NodeError(string message)
        {
            ViewBag.Title = "Claim Error";
            ViewBag.message = message;
            return View();
        }
    }
}