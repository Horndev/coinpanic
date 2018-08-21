using coinpanic_airdrop.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private static TimeSpan withdrawRateLimit = TimeSpan.FromSeconds(20);

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

        public bool TryWithdrawal(string payreq)
        {
            // Lock all threading
            lock(withdrawLock)
            {

            }
            return false;
        }
    }
}
