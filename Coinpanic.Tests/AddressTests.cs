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

        [TestMethod]
        public void TestBTCPPKH()
        {
            string BTCPaddr = "b1NkLyXdPm9iS2TZbxqJf46os6KJ4jFjNRn";

            var add = BitcoinAddress.Create(BTCPaddr, Network.BTCP);

            Assert.AreEqual(BTCPaddr, add.ToString());
        }

        [TestMethod]
        public void TestBTCP_PKH2PKH()
        {
            string BTCaddr = "14jkz2hJPqgqqKRhDqMYUx37CycQ7G6Ygy";
            string BTCPaddr = "b18CxfDYJ8ziwxbkGAynX5T1nqFKUR9X7pe";

            BitcoinAddress add = BitcoinAddress.Create(BTCaddr, Network.Main);

            var add2 = add.Convert(Network.BTCP);

            Assert.AreEqual(BTCPaddr, add2.ToString());
        }
        [TestMethod]
        public void TestBTCP_P2SHConvert()
        {
            string BTCaddr = "34ZuYSNSCm5Vtgtfn7PnxKYXP2rbp4N4rC";
            string BTCPaddr = "bxdzLB4rkNuk8nckkz48A3ViqiPq8cm5yFS";

            BitcoinAddress add = BitcoinAddress.Create(BTCaddr, Network.Main);

            var add2 = add.Convert(Network.BTCP);

            Assert.AreEqual(BTCPaddr, add2.ToString());
        }
    }
}