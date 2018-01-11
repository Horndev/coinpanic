using BitcoinLib.Services.Coins.Base;
using BitcoinLib.Services.Coins.Cryptocoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int rpc_port = Properties.Settings.Default.UBTCport;

            //Get server IP address
            //string externalip = new System.Net.WebClient().DownloadString("http://icanhazip.com");
            //Console.WriteLine(externalip);

            //Upload to website database.
            //TODO

            ICoinService B2XCoinService = new CryptocoinService(
                daemonUrl: "http://localhost:" + Properties.Settings.Default.B2Xport,
                rpcUsername: Properties.Settings.Default.B2Xuser,
                rpcPassword: Properties.Settings.Default.B2Xpw,
                walletPassword: Properties.Settings.Default.B2Xwalletpw,
                rpcRequestTimeoutInSeconds: 60);

            B2XCoinService.Parameters.CoinLongName = "Segwit 2X";
            B2XCoinService.Parameters.CoinShortName = "B2X";
            B2XCoinService.Parameters.IsoCurrencyCode = "B2X";

            var t = B2XCoinService.ListTransactions();
            var x = B2XCoinService.GetPeerInfo();
        }
    }
}
