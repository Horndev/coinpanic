using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using coinpanic_airdrop.Database;
using System.Linq;

namespace Coinpanic.Tests
{
    [TestClass]
    public class LNStatisticsTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            using (CoinpanicContext db = new CoinpanicContext())
            {
                // select * from LnTransaction where UserId = '13f8da23-a0d0-42e3-8c99-5f3c00878892'
                // select * from LNCJUser where TotalWithdrawn > TotalDeposited order by NumWithdraws desc
                // select * from LnTransaction as a inner join LnCJUser on a.UserId = LNCJUser.LnCJUserId order by TransactionId desc
                var c = db.LnTransactions.Count();
                var greedy = db.LnTransactions.ToDictionary(a => a, a => a);
                int z = 1;
            }
        }
    }
}
