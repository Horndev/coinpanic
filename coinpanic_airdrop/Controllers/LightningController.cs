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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static CoinpanicLib.Services.MailingService;

namespace coinpanic_airdrop.Controllers
{
    public class LightningController : Controller
    {
        /// <summary>
        /// This is the interface to a singleton payments service which is injected for IOC.
        /// </summary>
        public ILightningPayments paymentsService { get; private set; }

        /// <summary>
        /// Constructor with dependency injection for IOC and controller singleton control.
        /// </summary>
        /// <param name="paymentsService"></param>
        public LightningController(ILightningPayments paymentsService)
        {
            this.paymentsService = paymentsService;
        }

        // This listens for transactions which we are waiting for.
        private static ConcurrentDictionary<Guid, TransactionListener> lndTransactionListeners = new ConcurrentDictionary<Guid, TransactionListener>();

        private static bool usingTestnet = true;

        [HttpGet]
        public ActionResult Statistics()
        {
            using (CoinpanicContext db = new CoinpanicContext())
            {
                ViewBag.Errors = db.LnTransactions.AsNoTracking().Where(t => t.IsError).ToList();
            }

            return View();
        }

        public ActionResult CommunityJar(int page=1)
        {
            //return RedirectToAction("Maintenance", "Home");
            //return RedirectToAction(actionName: "Maintenance", controllerName:"Home");
            LndRpcClient lndClient = GetLndClient();

            // TODO: Added this try-catch to avoid errors
            ViewBag.URI = "03a9d79bcfab7feb0f24c3cd61a57f0f00de2225b6d31bce0bc4564efa3b1b5aaf@13.92.254.226:9735";

            string userId = SetOrUpdateUserCookie();

            // This will be the list of transactions shown to the user
            LnCJTransactions latestTx = new LnCJTransactions();
            using (CoinpanicContext db = new CoinpanicContext())
            {
                var jar = db.LnCommunityJars.AsNoTracking().Where(j => j.IsTestnet == usingTestnet).First();
                ViewBag.Balance = jar.Balance;
                int NumTransactions = jar.Transactions.Count();

                // Code for the paging
                ViewBag.NumTransactions = NumTransactions;
                ViewBag.NumPages = Math.Ceiling(Convert.ToDouble(NumTransactions) / 20.0);
                ViewBag.ActivePage = page;
                ViewBag.FirstPage = (page - 3) < 1 ? 1 : (page - 3);
                ViewBag.LastPage = (page + 3) < 6 ? 6 : (page + 3);

                //Get user
                string ip = GetClientIpAddress(Request);
                var user = GetUserFromDb(userId, db, jar, ip);
                var userMax = (user.TotalDeposited - user.TotalWithdrawn);
                if (userMax < 150)
                {
                    userMax = 150;
                }
                ViewBag.UserBalance = userMax;

                // Query and filter the transactions.  Cast into view model.
                latestTx.Transactions = jar.Transactions.OrderByDescending(t => t.TimestampSettled).Skip((page - 1) * 20).Take(20).Select(t => new LnCJTransaction()
                {
                    Timestamp = t.TimestampSettled == null ? DateTime.UtcNow : (DateTime)t.TimestampSettled,
                    Amount = t.Value,
                    Memo = t.Memo,
                    Type = t.IsDeposit ? "Deposit" : "Withdrawal",
                    Id = t.TransactionId,
                    Fee = t.FeePaid_Satoshi ?? -1,
                }).ToList();
                latestTx.Balance = jar.Balance;
            }
            return View(latestTx);
        }

        private string SetOrUpdateUserCookie()
        {
            string userId;
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

            return userId;
        }

        private static LndRpcClient GetLndClient()
        {
            usingTestnet = GetUseTestnet();
            var lndClient = new LndRpcClient(
                host: System.Configuration.ConfigurationManager.AppSettings[usingTestnet ? "LnTestnetHost" : "LnMainnetHost"],
                macaroonAdmin: System.Configuration.ConfigurationManager.AppSettings[usingTestnet ? "LnTestnetMacaroonAdmin" : "LnMainnetMacaroonAdmin"],
                macaroonRead: System.Configuration.ConfigurationManager.AppSettings[usingTestnet ? "LnTestnetMacaroonRead" : "LnMainnetMacaroonRead"],
                macaroonInvoice: System.Configuration.ConfigurationManager.AppSettings[usingTestnet ? "LnMainnetMacaroonInvoice" : "LnMainnetMacaroonInvoice"]);
            return lndClient;
        }

        private static LndRpcClient GetLndClient(bool useTestnet)
        {
            return new LndRpcClient(
                    host: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetHost" : "LnMainnetHost"],
                    macaroonAdmin: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetMacaroonAdmin" : "LnMainnetMacaroonAdmin"],
                    macaroonRead: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetMacaroonRead" : "LnMainnetMacaroonRead"],
                    macaroonInvoice: System.Configuration.ConfigurationManager.AppSettings[useTestnet ? "LnTestnetMacaroonInvoice" : "LnMainnetMacaroonInvoice"]);
        }

        public ActionResult WebWallet()
        {
            return View();
        }

        /// <summary>
        /// TODO: Move this to a dedicated controller
        /// </summary>
        /// <param name="qr"></param>
        /// <returns></returns>
        public ActionResult GetQR(string qr)
        {
            if (qr is null || qr == "")
                qr = "coinpanic.com";
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
            string ip = GetClientIpAddress(Request); ;
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

        public static string GetClientIpAddress(HttpRequestBase request)
        {
            try
            {
                var userHostAddress = request.UserHostAddress;

                // Attempt to parse.  If it fails, we catch below and return "0.0.0.0"
                // Could use TryParse instead, but I wanted to catch all exceptions
                IPAddress.Parse(userHostAddress);

                var xForwardedFor = request.ServerVariables["X_FORWARDED_FOR"];

                if (string.IsNullOrEmpty(xForwardedFor))
                    return userHostAddress;

                // Get a list of public ip addresses in the X_FORWARDED_FOR variable
                var publicForwardingIps = xForwardedFor.Split(',').Where(ip => !IsPrivateIpAddress(ip)).ToList();

                // If we found any, return the last one, otherwise return the user host address

                var retval = publicForwardingIps.Any() ? publicForwardingIps.Last() : userHostAddress;

                return retval;
            }
            catch (Exception)
            {
                // Always return all zeroes for any failure (my calling code expects it)
                return "0.0.0.0";
            }
        }

        private static bool IsPrivateIpAddress(string ipAddress)
        {
            // http://en.wikipedia.org/wiki/Private_network
            // Private IP Addresses are: 
            //  24-bit block: 10.0.0.0 through 10.255.255.255
            //  20-bit block: 172.16.0.0 through 172.31.255.255
            //  16-bit block: 192.168.0.0 through 192.168.255.255
            //  Link-local addresses: 169.254.0.0 through 169.254.255.255 (http://en.wikipedia.org/wiki/Link-local_address)

            var ip = IPAddress.Parse(ipAddress);
            var octets = ip.GetAddressBytes();

            var is24BitBlock = octets[0] == 10;
            if (is24BitBlock) return true; // Return to prevent further processing

            var is20BitBlock = octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31;
            if (is20BitBlock) return true; // Return to prevent further processing

            var is16BitBlock = octets[0] == 192 && octets[1] == 168;
            if (is16BitBlock) return true; // Return to prevent further processing

            var isLinkLocalAddress = octets[0] == 169 && octets[1] == 254;
            return isLinkLocalAddress;
        }

        private static DateTime timeLastAnonWithdraw = DateTime.Now - TimeSpan.FromHours(1);

        /// <summary>
        /// Pay the Community Jar payment request if it meets requirements of time restriction and value.
        /// </summary>
        /// <param name="request">LN payment request</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitPaymentRequest(string request)
        {
            //return RedirectToAction("Maintenance", "Home");
            int maxWithdraw = 150;
            int maxWithdraw_firstuser = 150;
            usingTestnet = GetUseTestnet();
            string ip = GetClientIpAddress(Request);

            var lndClient = GetLndClient();

            var paymentResult = paymentsService.TryWithdrawal(request);

            try
            {
                LnCJUser user;
                string userId = SetOrUpdateUserCookie();

                // Check if payment request is ok
                var decoded = lndClient.DecodePayment(request);
                
                if (decoded.destination == null)
                {
                    return Json(new { Result = "Error decoding invoice." });
                }

                // Check that there are funds in the Jar
                Int64 balance;
                LnCommunityJar jar;
                using (CoinpanicContext db = new CoinpanicContext())
                {
                    jar = db.LnCommunityJars.Where(j => j.IsTestnet == usingTestnet).AsNoTracking().First();
                    balance = jar.Balance;

                    if (Convert.ToInt64(decoded.num_satoshis) > balance)
                    {
                        return Json(new { Result = "Requested amount is greater than the available balance." });
                    }

                    //Get user
                    user = GetUserFromDb(userId, db, jar, ip);

                    var userMax = (user.TotalDeposited - user.TotalWithdrawn);
                    if (userMax < maxWithdraw)
                    {
                        userMax = maxWithdraw;
                    }

                    if (Convert.ToInt64(decoded.num_satoshis) > userMax)
                    {
                        return Json(new { Result = "Requested amount is greater than maximum allowed." });
                    }
                }

                // Check for banned nodes
                if (paymentsService.IsNodeBanned(decoded.destination, out string banmessage))
                {
                    return Json(new { Result = "Banned.  Reason: " + banmessage });
                }

                if (decoded.destination == "03a9d79bcfab7feb0f24c3cd61a57f0f00de2225b6d31bce0bc4564efa3b1b5aaf")
                {
                    return Json(new { Result = "Can not deposit from jar!"});
                }

                

                //Check rate limits
                bool isanon = false;
                using (CoinpanicContext db = new CoinpanicContext())
                {
                    //check if new user
                    DateTime? LastWithdraw = user.TimesampLastWithdraw;

                    //LastWithdraw = db.LnTransactions.Where(tx => tx.IsDeposit == false && tx.IsSettled == true && tx.UserId == user.LnCJUserId).OrderBy(tx => tx.TimestampCreated).AsNoTracking().First().TimestampCreated;
                    if (user.NumWithdraws == 0 && user.NumDeposits == 0)
                    {
                        maxWithdraw = maxWithdraw_firstuser;
                        isanon = true;
                        //check ip (if someone is not using cookies to rob the site)
                        var userIPs = db.LnCommunityJarUsers.Where(u => u.UserIP == ip).ToList();
                        if (userIPs.Count > 1)
                        {
                            //most recent withdraw
                            LastWithdraw = userIPs.Max(u => u.TimesampLastWithdraw);
                        }

                        // Re-check limits
                        if (Convert.ToInt64(decoded.num_satoshis) > maxWithdraw)
                        {
                            return Json(new { Result = "Requested amount is greater than maximum allowed for first time users (" + Convert.ToString(maxWithdraw) + ").  Make a deposit." });
                        }
                    }

                    if (user.TotalDeposited - user.TotalWithdrawn < maxWithdraw)
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

                    if (isanon && DateTime.Now - timeLastAnonWithdraw < TimeSpan.FromMinutes(60))
                    {
                        return Json(new { Result = "Too many first-time user withdraws.  You must wait another " + ((timeLastAnonWithdraw + TimeSpan.FromMinutes(60)) - DateTime.Now).TotalMinutes.ToString("0.0") + " minutes before withdrawing again, or make a deposit first." });

                    }
                }

                //all ok - make the payment

                SendPaymentResponse paymentresult;
                if (WithdrawRequests.TryAdd(request, DateTime.UtcNow))
                {
                    paymentresult = lndClient.PayInvoice(request);
                }
                else
                {
                    //double request!
                    Thread.Sleep(1000);

                    //Check if paid (in another thread)
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
                    // Save payment error to database
                    using (CoinpanicContext db = new CoinpanicContext())
                    {
                        user = GetUserFromDb(userId, db, jar, ip);
                        LnTransaction t = new LnTransaction()
                        {
                            UserId = user.LnCJUserId,
                            IsSettled = false,
                            Memo = decoded.description ?? "Withdraw",
                            Value = Convert.ToInt64(decoded.num_satoshis),
                            IsTestnet = GetUseTestnet(),
                            HashStr = decoded.payment_hash,
                            IsDeposit = false,
                            TimestampCreated = DateTime.UtcNow, //can't know
                            PaymentRequest = request,
                            DestinationPubKey = decoded.destination,
                            IsError = true,
                            ErrorMessage = paymentresult.payment_error,
                        };
                        db.LnTransactions.Add(t);
                        db.SaveChanges();
                    }
                    return Json(new { Result = "Payment Error: " + paymentresult.payment_error });
                }

                var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();

                using (CoinpanicContext db = new CoinpanicContext())
                {
                    user = GetUserFromDb(userId, db, jar, ip);
                    user.NumWithdraws += 1;
                    user.TotalWithdrawn += Convert.ToInt64(decoded.num_satoshis);
                    user.TimesampLastWithdraw = DateTime.UtcNow;

                    //insert transaction
                    LnTransaction t = new LnTransaction()
                    {
                        UserId = user.LnCJUserId,
                        IsSettled = true,
                        Memo = decoded.description == null ? "Withdraw" : decoded.description,
                        Value = Convert.ToInt64(decoded.num_satoshis),
                        IsTestnet = GetUseTestnet(),
                        HashStr = decoded.payment_hash,
                        IsDeposit = false,
                        TimestampSettled = DateTime.UtcNow,
                        TimestampCreated = DateTime.UtcNow, //can't know
                        PaymentRequest = request,
                        FeePaid_Satoshi = (paymentresult.payment_route.total_fees == null ? 0 : Convert.ToInt64(paymentresult.payment_route.total_fees)),
                        NumberOfHops = paymentresult.payment_route.hops == null ? 0 : paymentresult.payment_route.hops.Count(),
                        DestinationPubKey = decoded.destination,
                    };
                    db.LnTransactions.Add(t);
                    db.SaveChanges();

                    jar = db.LnCommunityJars.Where(j => j.IsTestnet == usingTestnet).First();
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
                        Id = t.TransactionId,
                    };

                    context.Clients.All.NotifyNewTransaction(newT);
                }

                if (isanon)
                {
                    timeLastAnonWithdraw = DateTime.Now;
                }

                return Json(new { Result = "success", Fees = (paymentresult.payment_route.total_fees == null ? "0" : paymentresult.payment_route.total_fees) });
            }
            catch (Exception e)
            {
                return Json(new { Result = "Error decoding request."});
            }
        }

        /// <summary>
        /// Query previous transactions and display
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult ShowTransaction(int id)
        {
            LnTransaction t = new LnTransaction();
            using (CoinpanicContext db = new CoinpanicContext())
            {
                var tr = db.LnTransactions.AsNoTracking().FirstOrDefault(tid => tid.TransactionId == id);
                if (tr != null)
                    t = tr;
            }
            return View(t);
        }

        [HttpPost]
        public ActionResult GetJarDepositInvoice(string amount, string memo)
        {
            string ip = GetClientIpAddress(Request); ;
            if (memo == null || memo == "")
            {
                memo = "Coinpanic Community Jar";
            }

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
                    DestinationPubKey = System.Configuration.ConfigurationManager.AppSettings["LnPubkey"],
                };
                db.LnTransactions.Add(t);
                db.SaveChanges();
            }

            // If a listener is not already running, this should start

            // Check if there is one already online.
            var numListeners = lndTransactionListeners.Count(kvp => kvp.Value.IsLive);

            // If we don't have one running - start it and subscribe
            if (numListeners < 1)
            {
                var listener = lndClient.GetListener();
                lndTransactionListeners.TryAdd(listener.ListenerId, listener);           //keep alive while we wait for payment
                listener.InvoicePaid += NotifyClientsInvoicePaid;     //handle payment message
                listener.StreamLost += OnListenerLost;                  //stream lost
                var a = new Task(() => listener.Start());                   //listen for payment
                a.Start();
            }
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
            lndTransactionListeners.TryRemove(l.ListenerId, out TransactionListener oldListener);
        }

        /// <summary>
        /// Notify web clients via Signalr that an invoice has been paid
        /// </summary>
        /// <param name="invoice"></param>
        private static void NotifyClientsInvoicePaid(Invoice invoice)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            bool isTesnet = GetUseTestnet();

            if (invoice.settle_date == "0" || invoice.settle_date == null)
            {
                // Was not settled
                return;
            }

            //Save in db
            using (CoinpanicContext db = new CoinpanicContext())
            {
                var jar = db.LnCommunityJars.Where(j => j.IsTestnet == isTesnet).First();

                //check if unsettled transaction exists
                var tx = db.LnTransactions.Where(tr => tr.PaymentRequest == invoice.payment_request).ToList();
                DateTime settletime = DateTime.UtcNow;

                LnTransaction t;
                if (tx.Count > 0)
                {
                    t = tx.First();
                    t.TimestampSettled = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc) + TimeSpan.FromSeconds(Convert.ToInt64(invoice.settle_date));
                    t.IsSettled = invoice.settled;
                }
                else
                {
                    //insert transaction
                    settletime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc) + TimeSpan.FromSeconds(Convert.ToInt64(invoice.settle_date));
                    t = new LnTransaction()
                    {
                        IsSettled = invoice.settled,
                        Memo = invoice.memo,
                        Value = Convert.ToInt64(invoice.value),
                        IsTestnet = GetUseTestnet(),
                        HashStr = invoice.r_hash,
                        IsDeposit = true,
                        TimestampSettled = settletime,
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

                t.IsSettled = invoice.settled;
                if (t.IsDeposit && t.IsSettled)
                {
                    jar.Balance += Convert.ToInt64(invoice.value);
                }
                jar.Transactions.Add(t);
                db.SaveChanges();

                //re-fetch to get the transaction id
                // Ok, this may not be required.
                //var tnew = db.LnTransactions.AsNoTracking().FirstOrDefault(tr => tr.PaymentRequest == invoice.payment_request && (DateTime)tr.TimestampSettled == settletime);

                //if (tnew != null)
                //    t = tnew;

                // Notify Web clients - this is shown to user

                // Client needs to check that the transaction received is theirs before marking successful.
                var newT = new LnCJTransaction()
                {
                    Timestamp = t.TimestampSettled == null ? DateTime.UtcNow : (DateTime)t.TimestampSettled,
                    Amount = t.Value,
                    Memo = t.Memo,
                    Type = t.IsDeposit ? "Deposit" : "Withdrawal",
                    Id = t.TransactionId,
                    Fee = t.FeePaid_Satoshi ?? -1,
                };

                context.Clients.All.NotifyNewTransaction(newT);
                if (invoice.settled)
                {
                    context.Clients.All.NotifyInvoicePaid(invoice.payment_request);
                }
                
            }
        }

        private static bool GetUseTestnet()
        {
            return System.Configuration.ConfigurationManager.AppSettings["LnUseTestnet"] == "true";
        }

        /// <summary>
        /// Lightning Status page
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
             return View();
        }


        // Caching
        private static TimeSpan StatusCacheTimeout = TimeSpan.FromMinutes(10);
        private static TimeSpan URICacheTimeout = TimeSpan.FromMinutes(10); // Not so expensive.
        private static TimeSpan FwdingCacheTimeout = TimeSpan.FromMinutes(10); // Not so expensive.
        private static DateTime LastNodeURIUpdate = DateTime.Now - URICacheTimeout - TimeSpan.FromMinutes(1);    // Initialize so that first call will set values
        private static DateTime LastNodeChannelsUpdate = DateTime.Now - StatusCacheTimeout - TimeSpan.FromMinutes(1);
        private static DateTime LastNodeSummaryUpdate = DateTime.Now - FwdingCacheTimeout - TimeSpan.FromMinutes(1);

        private static LnNodeURIViewModel nodeURIViewModel = new LnNodeURIViewModel() { Node_Pubkey = "", URI = "", Alias = "Coinpanic.com" };
        private static LnNodeSummaryViewModel nodeSummaryViewModel = new LnNodeSummaryViewModel();
        private static LnStatusViewModel nodeChannelViewModel = new LnStatusViewModel() { channels = new List<LnChannelInfoModel>() };

        private class UpdateTask
        {
            public Guid id;
            public Task task;
        }

        private static ConcurrentDictionary<Guid, UpdateTask> updateTasks = new ConcurrentDictionary<Guid, UpdateTask>();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult NodeURI()
        {
            // Check if cache expired
            if (DateTime.Now - LastNodeURIUpdate > URICacheTimeout)
            {
                // Update cache
                Guid taskid = Guid.NewGuid();
                UpdateTask updateTask = new UpdateTask()
                {
                    id = taskid,
                    task = new Task(() =>
                    {
                        try
                        {
                            bool useTestnet = GetUseTestnet();
                            LndRpcClient lndClient = GetLndClient(useTestnet);

                            var info = lndClient.GetInfo();
                            if (info == null)
                            {

                            }
                            else
                            {

                            }
                            nodeSummaryViewModel.NumChannelsActive = info.num_active_channels;
                            nodeSummaryViewModel.NumChannels = info.num_peers;
                            nodeURIViewModel.URI = info.uris.First();
                            nodeURIViewModel.Alias = info.alias;
                            nodeURIViewModel.Node_Pubkey = info.identity_pubkey;
                            UpdateTaskComplete(taskid);
                        }
                        catch (Exception e)
                        {
                            nodeURIViewModel.URI = "Error loading node information.";
                        }
                    }),
                };
                updateTasks.TryAdd(taskid, updateTask);
                updateTask.task.Start();

                LastNodeURIUpdate = DateTime.Now;
                if (nodeURIViewModel.URI == "")
                {
                    //wait for the task to finish.
                    while (updateTasks.ContainsKey(taskid))
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            return PartialView("NodeURI", nodeURIViewModel);
        }

        private static void UpdateTaskComplete(Guid id)
        {
            updateTasks.TryRemove(id, out UpdateTask t);
        }

        public ActionResult NodeChannels()
        {
            if (DateTime.Now - LastNodeChannelsUpdate > StatusCacheTimeout)
            {
                Guid taskid = Guid.NewGuid();
                UpdateTask updateTask = new UpdateTask()
                {
                    id = taskid,
                    task = new Task(() =>
                    {
                        try
                        {
                            bool useTestnet = GetUseTestnet();
                            LndRpcClient lndClient = GetLndClient(useTestnet);
                            string pubkey = nodeURIViewModel.Node_Pubkey;
                            if (pubkey == "") // If not already known
                            {
                                var info = lndClient.GetInfo();
                                pubkey = info.identity_pubkey;
                                nodeURIViewModel.URI = info.uris.First();
                                nodeURIViewModel.Alias = info.alias;
                                nodeURIViewModel.Node_Pubkey = info.identity_pubkey;
                            }

                            var channels = lndClient.GetChannels();

                            nodeChannelViewModel.channels = new List<LnChannelInfoModel>(); // Clear cache

                            using (CoinpanicContext db = new CoinpanicContext())
                            {
                                LnNode myNode = GetOrCreateNode(lndClient, nodeURIViewModel.Node_Pubkey, db);

                                //Check each channel
                                foreach (var c in channels.channels)
                                {
                                    LnChannelInfoModel channelViewModel = new LnChannelInfoModel();

                                    // Check if this is a new channel
                                    if (myNode.Channels.Where(ch => ch.ChannelId == c.chan_id).Count() < 1)
                                    {
                                        try
                                        {
                                            LnChannel thisChannel = GetOrCreateChannel(lndClient, db, c);

                                            if (thisChannel != null && !myNode.Channels.Contains(thisChannel))
                                            {
                                                myNode.Channels.Add(thisChannel);
                                                db.SaveChanges();
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            // TODO - manage errors reading channels
                                            LnChannel thisChannel = null;
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
                                    nodeChannelViewModel.channels.Add(channelViewModel);
                                }
                            }

                            // Updates to channelinfo
                            nodeSummaryViewModel.NumChannels = channels.channels.Count;
                            nodeSummaryViewModel.Capacity = Convert.ToDouble(channels.channels.Sum(c => Convert.ToInt64(c.capacity))) / 100000000.0;
                            nodeSummaryViewModel.LocalCapacity = Convert.ToDouble(channels.channels.Sum(n => Convert.ToInt64(n.local_balance))) / 100000000.0;
                            nodeSummaryViewModel.RemoteCapacity = Convert.ToDouble(channels.channels.Sum(n => Convert.ToInt64(n.remote_balance))) / 100000000.0;
                            nodeSummaryViewModel.ActiveCapacity = Convert.ToDouble(channels.channels.Where(c => c.active).Sum(c => Convert.ToInt64(c.capacity))) / 100000000.0;
                            nodeSummaryViewModel.ActiveLocalCapacity = Convert.ToDouble(channels.channels.Where(c => c.active).Sum(n => Convert.ToInt64(n.local_balance))) / 100000000.0;
                            nodeSummaryViewModel.ActiveRemoteCapacity = Convert.ToDouble(channels.channels.Where(c => c.active).Sum(n => Convert.ToInt64(n.remote_balance))) / 100000000.0;

                            UpdateTaskComplete(taskid);
                        }
                        catch (Exception e)
                        {
                            // Try again on next refresh
                            LastNodeChannelsUpdate = DateTime.Now - StatusCacheTimeout;
                        }
                    }),
                };
                updateTasks.TryAdd(taskid, updateTask);
                updateTask.task.Start();

                LastNodeChannelsUpdate = DateTime.Now;
            }

            return PartialView("NodeChannels", nodeChannelViewModel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult NodeSummary()
        {
            try
            {
                if (DateTime.Now - LastNodeSummaryUpdate > FwdingCacheTimeout)
                {
                    bool useTestnet = GetUseTestnet();
                    LndRpcClient lndClient = GetLndClient(useTestnet);
                    var xfers = lndClient.GetForwardingEvents();

                    //Total amount transferred
                    nodeSummaryViewModel.TotalValueXfer = Convert.ToDouble(xfers.forwarding_events.Sum(f => Convert.ToInt64(f.amt_out))) / 100000000.0;
                    nodeSummaryViewModel.NumXfer = xfers.forwarding_events.Count;
                    nodeSummaryViewModel.TotalFees = (Convert.ToDouble(xfers.forwarding_events.Sum(f => Convert.ToInt64(f.fee))) / 100000000.0).ToString("0.00000000");
                    LastNodeSummaryUpdate = DateTime.Now;
                }
            }
            catch (Exception e)
            {
                ViewBag.TotalValueXfer = "Unknown";
                ViewBag.NumXfer = "Unknown";
                ViewBag.TotalFees = "Unknown";
                MonitoringService.SendMessage("Lightning Error", e.Message);
            }

            return PartialView("NodeSummary", nodeSummaryViewModel);
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

            string chan_id = "";            // Temporary variable to use as the ChannelId identifier (should be unique) - used for db key
            if (c.chan_id == null)          // This sometimes happens for private channels.
            {
                chan_id = c.channel_point;  // this should always exist
            }
            else
            {
                chan_id = c.chan_id;        // Use value if reported
            }

            var chanFind = db.LnChannels.Where(ch => ch.ChannelId == chan_id);  
            if (chanFind.Count() < 1)
            {
                var chan = lndClient.GetChanInfo(chan_id);
                if (chan == null)
                {
                    var Node1 = GetOrCreateNode(lndClient, c.remote_pubkey, db);
                    var Node2 = GetOrCreateNode(lndClient, lndClient.GetInfo().identity_pubkey, db);
                    if (Node1 == null || Node2 == null)
                    {
                        // Bad node, can't find info in lnd database.
                        return null;
                    }

                    // not in database
                    thisChannel = new LnChannel()
                    {
                        Capacity = Convert.ToInt64(chan.capacity),
                        ChannelId = chan_id,
                        ChanPoint = chan.chan_point,
                        Node1 = Node1,
                        Node2 = Node2,
                    };
                }
                else
                {
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
                }
                
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
                if (nodeInfo.node == null)
                {
                    return null;
                }

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