using NBitcoin;
using QBitNinja.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinController
{
    public static class Bitcoin
    {
        public static bool IsValidAddress(string addr)
        {
            try
            {
                var address = BitcoinAddress.Create(addr, Network.Main);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static List<BitcoinAddress> ParseAddresses(List<string> addresses)
        {
            return addresses.Select(a => BitcoinAddress.Create(a, Network.Main)).ToList();
        }

        public static BitcoinAddress ParseAddress(string addresses)
        {
            return BitcoinAddress.Create(addresses, Network.Main);
        }

        public static string GenerateUnsignedTX(List<ICoin> UTXOs, List<Money> amounts, BitcoinAddress clientDepAddr, BitcoinAddress MyDepositAddr, Forks.ForkCode forkCode)
        {
            var builder = new TransactionBuilder();

            Transaction utx = builder
                .AddCoins(coins: UTXOs)
                .Send(clientDepAddr, amounts[0])
                .Send(MyDepositAddr, amounts[1])
                .SetChange(MyDepositAddr)
                .SendFees(amounts[2])
                .BuildTransaction(sign: false);

            if (forkCode == Forks.ForkCode.SBTC || forkCode == Forks.ForkCode.BTF)
            {
                utx.Version = 2;
            }

            return utx.ToHex();
        }

        public static string GetBlockData(List<ICoin> UTXOs)
        {
            var str = Serializer.ToString(UTXOs);
            return str;
        }
    }
}
