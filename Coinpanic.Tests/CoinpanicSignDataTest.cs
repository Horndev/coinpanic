using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoinController;
using System.Collections.Generic;
using NBitcoin;
using CoinpanicLib.Models;

namespace Coinpanic.Tests
{
    [TestClass]
    public class CoinpanicSignDataTest
    {
        [TestMethod]
        public void TestGenerate()
        {
            string coin = "BTP";

            //Create an unsigned transaction and encode 
            var scanner = new BlockScanner();

            List<string> addresslist = new List<string>()
            {
                "123qWRaufkCnUfh7WMAmJpKdFDw6zBQkn9",
            };
            var mydepaddr = "15Lo7GRtK7b8WaYQeynwRFU479FJBSyewr";

            var claimAddresses = Bitcoin.ParseAddresses(addresslist);
            Tuple<List<ICoin>, Dictionary<string, double>> claimcoins;

            claimcoins = scanner.GetUnspentTransactionOutputs(claimAddresses, coin, estimate: true);
            var amounts = scanner.CalculateOutputAmounts_Their_My_Fee(claimcoins.Item1, 0.05, 0.0003 * claimcoins.Item1.Count);
            // Generate unsigned tx

            var utx = Bitcoin.GenerateUnsignedTX(claimcoins.Item1, amounts, Bitcoin.ParseAddress(mydepaddr),
                Bitcoin.ParseAddress(mydepaddr),
                coin);

            //var w = Bitcoin.GetBlockData(claimcoins.Item1);

            BlockData bd = new BlockData()
            {
                fork = coin,
                coins = claimcoins.Item1,
                utx = utx,
            };

            string bdstr = NBitcoin.JsonConverters.Serializer.ToString(bd);
            System.IO.File.WriteAllText("D:\\ClaimData.txt", bdstr);
            Console.WriteLine(bdstr);
        }
    }
}
