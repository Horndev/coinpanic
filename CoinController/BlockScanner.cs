using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinController
{
    public class UTXO
    {
        //[DeserializeAs(Name = "address")]
        public string address { get; set; }

        //[DeserializeAs(Name = "txid")]
        public string txid { get; set; }

        public uint vout { get; set; }

        public string scriptPubKey { get; set; }

        public double amount { get; set; }

        public long satoshis { get; set; }

        public uint height { get; set; }

        public uint confirmations { get; set; }

    }

    public class BlockScanner
    {
        public BlockScanner()
        {
            client = new QBitNinjaClient(Network.Main);
            //client.Broadcast()
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public Tuple<List<ICoin>, Dictionary<string, double>> GetUnspentTransactionOutputs(List<BitcoinAddress> clientAddresses, string forkShortName)
        {
            List<ICoin> UTXOs = new List<ICoin>();
            List<ICoin> receivedCoins = new List<ICoin>();
            List<ICoin> spentCoins = new List<ICoin>();

            Dictionary<string, double> balances = new Dictionary<string, double>();

            foreach (var ca in clientAddresses)
            {
                List<ICoin> unspentCoins = new List<ICoin>();
                bool isSW = false;
                if ((ca as NBitcoin.BitcoinWitPubKeyAddress) != null && ((NBitcoin.BitcoinWitPubKeyAddress)ca).Type == Bech32Type.WITNESS_PUBKEY_ADDRESS)
                {
                    isSW = true;
                }
                if ((ca as NBitcoin.BitcoinWitPubKeyAddress) != null && ((NBitcoin.BitcoinWitPubKeyAddress)ca).Type != Bech32Type.WITNESS_SCRIPT_ADDRESS)
                {
                    isSW = true;
                }
                if (forkShortName == "SBTC" && !isSW)
                {
                    List<UTXO> addressUTXOs;
                    //Superbitcoin
                    var iclient = new RestClient();
                    iclient.BaseUrl = new Uri("http://block.superbtc.org/insight-api/");
                    var utxoRequest = new RestRequest("/addr/{addr}/utxo", Method.GET);
                    utxoRequest.AddUrlSegment("addr", ca);
                    utxoRequest.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
                    //utxoRequest.RootElement = "UTXOs";
                    IRestResponse<List<UTXO>> response = iclient.Execute<List<UTXO>>(utxoRequest);
                    //list of unspent transactions
                    addressUTXOs = response.Data;
                    if (addressUTXOs != null)
                    {
                        foreach (var utxo in addressUTXOs)
                        {
                            try
                            {
                                //create coin
                                var cOutPoint = new OutPoint(uint256.Parse(utxo.txid), (uint)utxo.vout);
                                var txout = new TxOut(new Money(satoshis: utxo.satoshis), new Script(StringToByteArray(utxo.scriptPubKey)));
                                ICoin coin = new Coin(fromOutpoint: cOutPoint, fromTxOut: txout);// //Coin(fromTxHash: uint256.Parse(utxo.txid), fromOutputIndex: utxo.vout, amount: new Money(satoshis: utxo.satoshis), scriptPubKey: new Script(utxo.scriptPubKey));
                                unspentCoins.Add(coin);
                            }
                            catch (Exception e)
                            {
                                int z = 1;
                            }
                        }
                        UTXOs.AddRange(unspentCoins);
                    }
                }
                //if bech32, need to use QBitNinjaClient.  Otherwise, we can read the chain directly...
                else
                if (true)
                {
                    BalanceModel AddressTxs = client.GetBalance(ca).Result;
                    //Select those operations which are before the fork (i.e. have a balance)
                    int blockheight = NBitcoin.Forks.BitcoinForks.ForkByShortName[forkShortName].Height;//Forks.ForkBlock[forkCode];

                    List<BalanceOperation> B2XValid_Operations = AddressTxs.Operations.Where(o => o.Height < blockheight).ToList();

                    //Get the UTXOs
                    // see https://github.com/ProgrammingBlockchain/ProgrammingBlockchain/blob/master/bitcoin_transfer/transaction.md
                    receivedCoins = B2XValid_Operations.SelectMany(o => o.ReceivedCoins).ToList();
                    spentCoins = B2XValid_Operations.SelectMany(o => o.SpentCoins).ToList();

                    unspentCoins = receivedCoins.Where(c => spentCoins.Select(sc => sc.Outpoint.Hash).Contains(c.Outpoint.Hash) == false).ToList();
                    UTXOs.AddRange(unspentCoins);
                }
                var value = unspentCoins.Sum(o => ((Money)o.Amount).ToDecimal(MoneyUnit.BTC));
                if (!balances.ContainsKey(ca.ToString()))
                {
                    balances.Add(ca.ToString(), Convert.ToDouble(value));
                }
                
            }
            Tuple<List<ICoin>, Dictionary<string, double>> res = new Tuple<List<ICoin>, Dictionary<string, double>>(UTXOs, balances);

            return res;
        }

        public List<Money> CalculateOutputAmounts_Their_My_Fee(List<ICoin> UTXOs, double myfee_percent, double miner_txfee)
        {
            var total_input_amount = UTXOs.Sum(o => ((Money)o.Amount).ToDecimal(MoneyUnit.BTC));
            var output_myfee_amount = Convert.ToDouble(total_input_amount) * myfee_percent;
            var output_client_amount = Convert.ToDouble(total_input_amount) - output_myfee_amount - miner_txfee;

            var theirMoney = Money.Coins(Convert.ToDecimal(output_client_amount));
            var myMoney = Money.Coins(Convert.ToDecimal(output_myfee_amount));
            var txFees = Money.Coins(Convert.ToDecimal(miner_txfee));

            List<Money> result = new List<Money>()
            {
                theirMoney,
                myMoney,
                txFees
            };

            return result;
        }
        
    //    public string CreateUnsignedTransaction(List<ICoin> UTXOs, BitcoinAddress clientDepAddr, Money theirMoney, BitcoinAddress myDepAddr, Money myMoney, Money txFees, Forks.ForkCode forkCode)
    //    {
    //        var builder = new TransactionBuilder();

    //        Transaction utx = builder
				//.AddCoins(coins: UTXOs)
				//.Send(clientDepAddr, theirMoney)
				//.Send(myDepAddr, myMoney)
				//.SetChange(myDepAddr)
				//.SendFees(txFees)
				////.SetLockTime(LockTime.Zero)
				//.BuildTransaction(sign: false);

    //        if (forkCode == Forks.ForkCode.SBTC)
    //        {
    //            utx.Version = 2;
    //        }

    //        return utx.ToHex();
    //    }

        public string GetWitnessText(List<ICoin> UTXOs)
        {
            var str = Serializer.ToString(UTXOs);
            return str;
        }
        
        private QBitNinjaClient client;
    }
}
