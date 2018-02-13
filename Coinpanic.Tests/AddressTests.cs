using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;

namespace Coinpanic.Tests
{
    [TestClass]
    public class AddressTests
    {
        [TestMethod]
        public void TestBTGP2PKH()
        {
            string BTGaddr = "GZGmgmawaDLjoaz4ZQsz9VXNE9qCoX5oY6";

            var add = BitcoinAddress.Create(BTGaddr, Network.BTG);

            Assert.AreEqual(BTGaddr, add.ToString());
        }

        [TestMethod]
        public void TestBTPP2PKH()
        {
            string BTPaddr = "PbQdinKhh9pRKTfkJuhngfX8ctQiZ5cNvy";

            var add = BitcoinAddress.Create(BTPaddr, Network.BTP);

            Assert.AreEqual(BTPaddr, add.ToString());
        }

        [TestMethod]
        public void TestBTPP2SH()
        {
            string BTPaddr = "Qf1NJsJ8Nf34pNcYQCCDCmPi8doa3rcoWR";

            var add = BitcoinAddress.Create(BTPaddr, Network.BTP);

            Assert.AreEqual(BTPaddr, add.ToString());
        }

        [TestMethod]
        public void TestBPAPKH()
        {
            string BPAaddr = "PPu3XYFn8AhVvXjzrnjBCb5FTim8X3RX8u";

            var add = BitcoinAddress.Create(BPAaddr, Network.BPA);

            Assert.AreEqual(BPAaddr, add.ToString());
        }
    }
}
