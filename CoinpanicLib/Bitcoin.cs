﻿using NBitcoin;
using NBitcoin.Forks;
using QBitNinja.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinController
{
    public static class Bitcoin
    {
        public static bool IsValidAddress(string addr, string coin = "BTC", Network n = null)
        {
            try
            {
                if (n == null)
                {
                    n = Network.Main;
                }
                var address = BitcoinAddress.Create(addr, n);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
            return true;
        }

        public static List<BitcoinAddress> ParseAddresses(List<string> addresses)
        {
            return addresses.Where(a => a.Length > 30).Select(a => BitcoinAddress.Create(a.Trim(' '), Network.Main)).ToList();
        }

        public static BitcoinAddress ParseAddress(string addresses, string coin = "BTC", Network n = null)
        {
            if (n == null)
            {
                n = Network.Main;
            }
            return BitcoinAddress.Create(addresses, n);
        }

        public static string GenerateUnsignedTX(List<ICoin> UTXOs, List<Money> amounts, BitcoinAddress clientDepAddr, BitcoinAddress MyDepositAddr, string forkShortName)
        {
            var builder = new TransactionBuilder();

            if (amounts[2].Satoshi > amounts[0].Satoshi+ amounts[1].Satoshi)
            {
                //too small of a transaction
                return "";
            }

            Transaction utx;

            if (clientDepAddr.ToString() == MyDepositAddr.ToString())
            {
                utx = builder
                    .AddCoins(coins: UTXOs)
                    .Send(MyDepositAddr, amounts[0] + amounts[1])                
                    .SetChange(MyDepositAddr)
                    .SendFees(amounts[2])
                    .BuildTransaction(sign: false);
            }
            else
            {
                utx = builder
                    .AddCoins(coins: UTXOs)
                    .Send(clientDepAddr, amounts[0])
                    .Send(MyDepositAddr, amounts[1])
                    .SetChange(MyDepositAddr)
                    .SendFees(amounts[2])
                    .BuildTransaction(sign: false);
            }
            utx.Version = BitcoinForks.ForkByShortName[forkShortName].TransactionVersion;
            return utx.ToHex();
        }

        public static string GetBlockData(List<ICoin> UTXOs)
        {
            var str = Serializer.ToString(UTXOs);
            return str;
        }
    }
}
