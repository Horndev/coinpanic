using coinpanic_airdrop.Database;
using coinpanic_airdrop.Models;
using LightningLib.lndrpc;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace coinpanic_airdrop.Services
{
    public class CJLightningPayments : ILightningPayments
    {
        /// <summary>
        /// Tracks the time each node has last withdrawn.
        /// </summary>
        private static ConcurrentDictionary<string, DateTime> nodeWithdrawAttemptTimes = new ConcurrentDictionary<string, DateTime>();
        private static ConcurrentDictionary<string, DateTime> userWithdrawAttemptTimes = new ConcurrentDictionary<string, DateTime>();

        // Badness value for node (for banning)
        private static ConcurrentDictionary<string, int> nodeBadness = new ConcurrentDictionary<string, int>();

        private static TimeSpan withdrawRateLimit = TimeSpan.FromHours(6);//.FromSeconds(60);

        private static DateTime timeLastAnonWithdraw = DateTime.Now - TimeSpan.FromHours(1);

        //Used for rate limiting double withdraws
        static ConcurrentDictionary<string, DateTime> WithdrawRequests = new ConcurrentDictionary<string, DateTime>();


        /// <summary>
        /// Ensure only one withdraw at a time
        /// </summary>
        private Object withdrawLock = new Object();

        public CJLightningPayments()
        {

        }

        public bool IsNodeBanned(string node, out string message)
        {
            // TODO: This should be in a database with admin view
            Dictionary<string, string> bannedNodes = new Dictionary<string, string>()
                {
                    { "023216c5b9a54b6179645c76b279ae267f3c6b2379b9f305d57c75065006a8e5bd", "Abusive use - Scripted withdraws to drain jar" },
                    { "0370373fd498ffaf16dc0cf46250c5dae76fd79b0592254bf26fa74de815898a21", "Abusive use - Scripted withdraws to drain jar" },
                    { "0229cf81c21bbd21c2a41a4ae645933b89bb6d9a5920ca90e41ba270666879adab", "Abusive use - scripted withdraws to drain jar" },
                    { "02db6ef942d4c89396d4c8ef2499654e01f32bc795cfe0d6fdd58ee6d8a89f9bdc", "Abusive use - scripted withdraws to drain jar" },
                    { "0209899301a36435fc402690adaea98ec10ce03a411834b3dad4397f771d27a25a", "Abusive use - scripted withdraws to drain jar" },
                    { "03389eef6764322287cec981e05bbd9feefb9fb733d26f309aba5055beb10de5fb", "Abusive use - scripted withdraws to drain jar" },
                    { "034faddc9d135d1d4d1cbf9be0567b24d1b7711056736310c01b8caa14ea00578d", "Abusive use - scripted withdraws to drain jar" },
                    { "02c108d545c270c7958e9825ecc6b5a5622194064300d1804c1998a1c6304a08dd", "Abusive use - scripted withdraws to drain jar" },
                    { "03035bbc31c789d0571630b93cb2cf58deca7ff137a040bf979a58eaa267d47141", "Abusive use - scripted withdraws to drain jar" },
                    { "03d13347b580a3b27d3d532ca4571ca50789b92ed3495d939eb688559abfcf6162", "Abusive use - excessive scripting withdraws to drain jar.  Only one per hour without deposit." },
                    { "0345aeb81c9a06d198d2c959745fd689bcb5be5c4418a2efe0d2975943046c71ad", "Abusive use - scripted withdraws to drain jar" },
                    { "028b41a763b15bfbb097bb8c3f72793c886b88a7fe1460c03244d26a97bb9f5604", "Abusive use - scripted withdraws to drain jar" },
                };

            if (bannedNodes.Keys.Contains(node))
            {
                message = bannedNodes[node];
                return true;
            }
            message = "";
            return false;
        }

        public void RecordNodeWithdraw(string node)
        {
            nodeWithdrawAttemptTimes.AddOrUpdate(
                node,                               // node of interest
                DateTime.UtcNow,                    // Value to insert if new node
                (key, oldval) => DateTime.UtcNow);  // Update function if existing node
        }

        public string Test()
        {
            using (CoinpanicContext db = new CoinpanicContext())
            {
                var jar = db.LnCommunityJars.AsNoTracking().Where(j => j.IsTestnet == false).FirstOrDefault();
                if (jar == null)
                {
                    return "Jar not found";
                }
                return Convert.ToString(jar.Balance);
            } 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="userId"></param>
        /// <param name="lndClient"></param>
        /// <returns></returns>
        public object TryWithdrawal(string request, string userId, string ip, LndRpcClient lndClient)
        {
            int maxWithdraw = 150;
            int maxWithdraw_firstuser = 150;

            if (lndClient == null)
            {
                throw new ArgumentNullException(nameof(lndClient));
            }

            // Lock all threading
            lock (withdrawLock)
            {
                LnCJUser user;
                try
                {
                    var decoded = lndClient.DecodePayment(request);

                    // Check if payment request is ok
                    if (decoded.destination == null)
                    {
                        return new { Result = "Error decoding invoice." };
                    }

                    // Check that there are funds in the Jar
                    Int64 balance;
                    LnCommunityJar jar;
                    using (CoinpanicContext db = new CoinpanicContext())
                    {
                        jar = db.LnCommunityJars
                            .Where(j => j.IsTestnet == false)
                            .AsNoTracking().First();

                        balance = jar.Balance;

                        if (Convert.ToInt64(decoded.num_satoshis) > balance)
                        {
                            return new { Result = "Requested amount is greater than the available balance." };
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
                            return new { Result = "Requested amount is greater than maximum allowed." };
                        }
                    }

                    // Check for banned nodes
                    if (IsNodeBanned(decoded.destination, out string banmessage))
                    {
                        return new { Result = "Banned.  Reason: " + banmessage };
                    }

                    if (decoded.destination == "03a9d79bcfab7feb0f24c3cd61a57f0f00de2225b6d31bce0bc4564efa3b1b5aaf")
                    {
                        return new { Result = "Can not deposit from jar!" };
                    }

                    //Check rate limits

                    if (nodeWithdrawAttemptTimes.TryGetValue(decoded.destination, out DateTime lastWithdraw))
                    {
                        if ((DateTime.UtcNow - lastWithdraw) < withdrawRateLimit)
                        {
                            return new { Result = "Rate limit exceeded." };
                        }
                    }

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
                                return new { Result = "Requested amount is greater than maximum allowed for first time users (" + Convert.ToString(maxWithdraw) + ").  Make a deposit." };
                            }
                        }

                        if (user.TotalDeposited - user.TotalWithdrawn < maxWithdraw)
                        {
                            //check for time rate limiting
                            if (DateTime.UtcNow - LastWithdraw < TimeSpan.FromHours(1))
                            {
                                return new { Result = "You must wait another " + ((user.TimesampLastWithdraw + TimeSpan.FromHours(1)) - DateTime.UtcNow).Value.TotalMinutes.ToString("0.0") + " minutes before withdrawing again, or make a deposit first." };
                            }
                        }

                        //Check if already paid
                        if (db.LnTransactions.Where(tx => tx.PaymentRequest == request && tx.IsSettled).Count() > 0)
                        {
                            return new { Result = "Invoice has already been paid." };
                        }

                        if (isanon && DateTime.Now - timeLastAnonWithdraw < TimeSpan.FromMinutes(60))
                        {
                            return new { Result = "Too many first-time user withdraws.  You must wait another " + ((timeLastAnonWithdraw + TimeSpan.FromMinutes(60)) - DateTime.Now).TotalMinutes.ToString("0.0") + " minutes before withdrawing again, or make a deposit first." };

                        }
                    }

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
                                return new { Result = "success", Fees = "0" };
                            }

                            return new { Result = "Please click only once.  Payment already in processing." };
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
                                IsTestnet = false,
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
                        return new { Result = "Payment Error: " + paymentresult.payment_error };
                    }

                    // We have a successful payment

                    // Record time of withdraw to the node
                    nodeWithdrawAttemptTimes.TryAdd(decoded.destination, DateTime.UtcNow);

                    // Notify client(s)
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
                            IsTestnet = false,
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

                        jar = db.LnCommunityJars.Where(j => j.IsTestnet == false).First();
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

                    return new { Result = "success", Fees = (paymentresult.payment_route.total_fees == null ? "0" : paymentresult.payment_route.total_fees) };
                }
                catch (Exception e)
                {
                    return new { Result = "Error decoding request." };
                }
            }
            return new { Result = "Error decoding request." };
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
    }
}
