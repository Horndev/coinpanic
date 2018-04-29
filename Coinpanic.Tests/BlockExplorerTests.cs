using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using CoinController;
using System.Collections.Generic;
using RestSharp;

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

        [TestMethod]
        public void Explorer_DecodeTx_P2PKH_BCA()
        {
            var url = "https://explorer.bitcoinatom.io/";

            var client = new RestClient(url);

            var req = new RestRequest("/rpc-terminal/", Method.POST);
            req.AddHeader("content-type", "application/x-www-form-urlencoded");
            req.AddHeader("x-requested-with", "XMLHttpRequest");

            req.AddObject(new { cmd= "decoderawtransaction 0100000002c12cb5947c620606e41f4bc4e1f7785e8d447c3cd05c39a9f47137eaab8d3e360000000082483045022100a1686122d8cb70cd3ff5df93f368716a3cc983f9edd974add2d2f9a58e43e77b022045c0d687aec4c3c4eb156857b923c473873f08e063c5415c3a3bcafa134fa5e4412103e58f6fb99160b304b93d14ee34e83d7bac1b83542164a45980d6d4471da9d15c160014f92bafe40657af61565b45181e9880f49b2b7611ffffffffa578baf1c6d25880a64f50679d3c43d848195178b444af9a97f06e917048818c0000000081473044022011cc993bbb3b4207be2b8f8c1ad2d4f1f0e2f97bb121920fa1e20ad5399b366402203ed5a639ba46a4976b860fd093e5003133f8039355055511d411b5ee5f3a874d412103749831d4b4820ef4eccd2626f3b827a822e6d7726332d4ba8a3c2447cc96f95516001487c8ed953eb72e9313fb60435cbb19ba56c5882effffffff0207f88c1e000000001976a9140c1d8f6dd978a6957289df4814108ea8e37ca69e88ac99ad9b01000000001976a914b3b255028648e151b3e419ab6c5b2e9656ba363988ac00000000" });

            var response = client.Execute(req);
            string responseStr = response.Content;

            
            int z = 1;
        }
    }
}
