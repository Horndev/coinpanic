using BitcoinLib.ExceptionHandling.Rpc;
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

            // Start with --rpcport=[port]

            ICoinService B2XCoinService = new CryptocoinService(
                daemonUrl: "http://localhost:" + Properties.Settings.Default.B2Xport,
                rpcUsername: Properties.Settings.Default.B2Xuser,
                rpcPassword: Properties.Settings.Default.B2Xpw,
                walletPassword: Properties.Settings.Default.B2Xwalletpw,
                rpcRequestTimeoutInSeconds: 60);

            B2XCoinService.Parameters.CoinLongName = "Segwit 2X";
            B2XCoinService.Parameters.CoinShortName = "B2X";
            B2XCoinService.Parameters.IsoCurrencyCode = "B2X";
            try
            {
                var x = B2XCoinService.GetPeerInfo();
                var t = B2XCoinService.ListTransactions();
                
            }
            catch (RpcInternalServerErrorException exception)
            {
                var errorCode = 0;
                var errorMessage = string.Empty;

                        if (exception.RpcErrorCode.GetHashCode() != 0)
                        {
                            errorCode = exception.RpcErrorCode.GetHashCode();
                            errorMessage = exception.RpcErrorCode.ToString();
                        }
                Console.WriteLine("[Failed] {0} {1} {2}", exception.Message, errorCode != 0 ? "Error code: " + errorCode : string.Empty, !string.IsNullOrWhiteSpace(errorMessage) ? errorMessage : string.Empty);
            }
            catch (Exception exception)
            {
                Console.WriteLine("[Failed]\n\nPlease check your configuration and make sure that the daemon is up and running and that it is synchronized. \n\nException: " + exception);
            }
        }
    }
}
