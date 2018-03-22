using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using CoinController;
using System.Collections.Generic;

namespace Coinpanic.Tests
{
    [TestClass]
    public class BlockExplorerTests
    {
        [TestMethod]
        public void TestBTCP()
        {
            string BTCaddr = "38q354WHtHUDFeVXCh4nkQ2UtL66M3FVnK";

            BitcoinAddress ca = BitcoinAddress.Create(BTCaddr, Network.Main);
            
            var btcpa = ca.Convert(Network.BTCP);
            string baseURL = "https://explorer.btcprivate.org/api";
            List<ICoin> UTXOs = new List<ICoin>();
            var unspentCoins = BlockScanner.GetUTXOFromInsight(UTXOs, btcpa, baseURL);

            int z = 1;
        }
    }
}
