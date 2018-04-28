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
        public void Explorer_Query_P2SH_BTCP()
        {
            string BTCaddr = "38q354WHtHUDFeVXCh4nkQ2UtL66M3FVnK";

            BitcoinAddress ca = BitcoinAddress.Create(BTCaddr, Network.Main);
            var btcpa = ca.Convert(Network.BTCP);
            string baseURL = "https://explorer.btcprivate.org/api";
            List<ICoin> UTXOs = new List<ICoin>();
            var unspentCoins = BlockScanner.GetUTXOFromInsight(UTXOs, btcpa, baseURL);

            int z = 1;
        }

        [TestMethod]
        public void Explorer_Query_P2PKH_BCI()
        {
            string BTCaddr = "14jkz2hJPqgqqKRhDqMYUx37CycQ7G6Ygy";
            BitcoinAddress ca = BitcoinAddress.Create(BTCaddr, Network.Main);
            var addr = ca.Convert(Network.BCI);
            string baseURL = "https://explorer.bitcoininterest.io/api/";
            List<ICoin> UTXOs = new List<ICoin>();
            var unspentCoins = BlockScanner.GetUTXOFromInsight(UTXOs, addr, baseURL);
            int z = 1;
        }
    }
}
