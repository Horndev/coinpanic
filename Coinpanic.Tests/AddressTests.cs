using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;

namespace Coinpanic.Tests
{
    [TestClass]
    public class AddressTests
    {
        [TestMethod]
        public void Fork_Address_Convert_CashAddr_BCH()
        {
            string cashAddr = "bitcoincash:qr3vw2jaxaqadyq50udtskpw23h276t28u95ch0uwn";
            string BTCaddr = SharpCashAddr.Converter.cashAddrToOldAddr(cashAddr, out bool isP2PKH, out _);
            Assert.AreEqual("1Mg6Jcd3mEGTmKFoH9NGuaMs6nEDBPJkAf", BTCaddr);
        }

        [TestMethod]
        public void Fork_Address_Create_P2PKH_BBC()
        {
            string BBCaddr = "BFsvMogLzGeAiek3zYRzNMbKT4UTt6D7wp";
            var add = BitcoinAddress.Create(BBCaddr, Network.BBC);
            Assert.AreEqual(BBCaddr, add.ToString());
        }

        [TestMethod]
        public void Fork_Address_Create_P2PKH_BTG()
        {
            string BTGaddr = "GZGmgmawaDLjoaz4ZQsz9VXNE9qCoX5oY6";
            var add = BitcoinAddress.Create(BTGaddr, Network.BTG);
            Assert.AreEqual(BTGaddr, add.ToString());
        }

        [TestMethod]
        public void Fork_Address_Create_P2PKH_BCI()
        {
            string addr = "iCJB7egYMV5S2xs9CyzBiJtq8r1e5R2isc";
            var add = BitcoinAddress.Create(addr, Network.BCI);
            Assert.AreEqual(addr, add.ToString());
        }

        [TestMethod]
        public void Fork_Address_Create_P2PKH_BCA()
        {
            string addr = "AZyMH7m2YfbGLatXYbKLAv5ubWPcoVTush";
            var add = BitcoinAddress.Create(addr, Network.BCA);
            Assert.AreEqual(addr, add.ToString());
        }

        [TestMethod]
        public void Fork_Address_Create_P2SH_BCL()
        {
            string BCLaddr = "3JQ5RMJmFX58DzJZZzJwnQDWaKuBXVEUDH";
            var add = BitcoinAddress.Create(BCLaddr, Network.BCL);
            Assert.AreEqual(BCLaddr, add.ToString());
        }

        [TestMethod]
        public void Fork_Address_Create_P2PKH_BTP()
        {
            string BTPaddr = "PbQdinKhh9pRKTfkJuhngfX8ctQiZ5cNvy";
            var add = BitcoinAddress.Create(BTPaddr, Network.BTP);
            Assert.AreEqual(BTPaddr, add.ToString());
        }

        [TestMethod]
        public void Fork_Address_Create_P2SH_BTP()
        {
            string BTPaddr = "Qf1NJsJ8Nf34pNcYQCCDCmPi8doa3rcoWR";
            var add = BitcoinAddress.Create(BTPaddr, Network.BTP);
            Assert.AreEqual(BTPaddr, add.ToString());
        }

        [TestMethod]
        public void Fork_Address_Create_P2PKH_BPA()
        {
            string BPAaddr = "PPu3XYFn8AhVvXjzrnjBCb5FTim8X3RX8u";
            var add = BitcoinAddress.Create(BPAaddr, Network.BPA);
            Assert.AreEqual(BPAaddr, add.ToString());
        }

        [TestMethod]
        public void Fork_Address_Create_P2PKH_BTCP()
        {
            string BTCPaddr = "b1NkLyXdPm9iS2TZbxqJf46os6KJ4jFjNRn";

            var add = BitcoinAddress.Create(BTCPaddr, Network.BTCP);

            Assert.AreEqual(BTCPaddr, add.ToString());
        }

        [TestMethod]
        public void Fork_Address_Convert_P2PKH_BTCP()
        {
            string BTCaddr = "14jkz2hJPqgqqKRhDqMYUx37CycQ7G6Ygy";
            string BTCPaddr = "b18CxfDYJ8ziwxbkGAynX5T1nqFKUR9X7pe";
            BitcoinAddress add = BitcoinAddress.Create(BTCaddr, Network.Main);
            var add2 = add.Convert(Network.BTCP);
            Assert.AreEqual(BTCPaddr, add2.ToString());
        }

        [TestMethod]
        public void Fork_Address_Convert_P2SH_BTCP()
        {
            string BTCaddr = "34ZuYSNSCm5Vtgtfn7PnxKYXP2rbp4N4rC";
            string BTCPaddr = "bxdzLB4rkNuk8nckkz48A3ViqiPq8cm5yFS";
            BitcoinAddress add = BitcoinAddress.Create(BTCaddr, Network.Main);
            var add2 = add.Convert(Network.BTCP);
            Assert.AreEqual(BTCPaddr, add2.ToString());
        }

        [TestMethod]
        public void Fork_Address_Convert_P2PKH_BCI()
        {
            string BTCaddr = "14jkz2hJPqgqqKRhDqMYUx37CycQ7G6Ygy";
            string ConvertedAddress = "i7DHR76gpExDFXeXidL3wjoMQRtcf4JUsG";
            BitcoinAddress add = BitcoinAddress.Create(BTCaddr, Network.Main);
            var add2 = add.Convert(Network.BCI);
            Assert.AreEqual(ConvertedAddress, add2.ToString());
        }

        [TestMethod]
        public void Fork_Address_Convert_P2PKH_BCA()
        {
            string BTCaddr = "14jkz2hJPqgqqKRhDqMYUx37CycQ7G6Ygy";
            string ConvertedAddress = "AKWddXYvizLzeHdgnV1sdqJCgaY6Ranszd";
            BitcoinAddress add = BitcoinAddress.Create(BTCaddr, Network.Main);
            var add2 = add.Convert(Network.BCA);
            Assert.AreEqual(ConvertedAddress, add2.ToString());
        }

        [TestMethod]
        public void Fork_Address_Convert_P2SH_BCI()
        {
            string BTCaddr = "34KizF1eXu7rN8geRvTCPJ3sc5iW74KTwa";
            string ConvertedAddress = "AJQaiCNqK9Td5wCCsUSw7Yx2wAMUuUedHZ";
            BitcoinAddress add = BitcoinAddress.Create(BTCaddr, Network.Main);
            var add2 = add.Convert(Network.BCI);
            Assert.AreEqual(ConvertedAddress, add2.ToString());
        }
    }
}