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
                //if (forkShortName == "BCX" && !isSW)
                //{
                //    string baseURL = "https://bcx.info/insight-api";
                //    unspentCoins = GetUTXOFromInsight(UTXOs, ca, baseURL).Distinct().ToList();
                //}
                else if (forkShortName == "B2X" && !isSW)
                {
                    string baseURL = "https://explorer.b2x-segwit.io/b2x-insight-api";
                    unspentCoins = GetUTXOFromInsight(UTXOs, ca, baseURL);
                }
                else if (forkShortName == "BCH" && !isSW)
                {
                    List<UTXO> addressUTXOs;
                    var iclient = new RestClient();
                    iclient.BaseUrl = new Uri("https://blockdozer.com/insight-api/");
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
                else if (forkShortName == "SBTC" && !isSW)
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
                {
                    BalanceModel AddressTxs = client.GetBalance(ca).Result;
                    //Select those operations which are before the fork (i.e. have a balance)
                    int blockheight = NBitcoin.Forks.BitcoinForks.ForkByShortName[forkShortName].Height;//Forks.ForkBlock[forkCode];

                    List<BalanceOperation> Valid_Operations = AddressTxs.Operations.Where(o => o.Height < blockheight).ToList(); ;
                    if (forkShortName == "UBTC")
                    {
                        //there must be some activity in this period, otherwise all UTXOs > 0.01 BTC at fork height will be taken
                        List<BalanceOperation> Phase1_ForkOps = AddressTxs.Operations.Where(o => o.Height > blockheight && o.Height < 501878).ToList();

                        //need outgoing transaction not to be robbed
                        bool robbed = Phase1_ForkOps.Where(o=>o.SpentCoins.Count() > 0).Count() == 0;
                        if (robbed)
                        {
                            receivedCoins = Valid_Operations.SelectMany(o => o.ReceivedCoins).Where(c => ((Money)c.Amount).ToDecimal(MoneyUnit.BTC) < Convert.ToDecimal(0.01)).ToList();
                        }
                        else
                        {
                            receivedCoins = Valid_Operations.SelectMany(o => o.ReceivedCoins).Where(c => ((Money)c.Amount).ToDecimal(MoneyUnit.BTC) < Convert.ToDecimal(0.01)).ToList();
                        }
                        //don't need to modify since later logic takes care of UTXOs
                        spentCoins = Valid_Operations.SelectMany(o => o.SpentCoins).ToList();
                    }
                    else
                    {
                        //Get the UTXOs
                        // see https://github.com/ProgrammingBlockchain/ProgrammingBlockchain/blob/master/bitcoin_transfer/transaction.md
                        receivedCoins = Valid_Operations.SelectMany(o => o.ReceivedCoins).ToList();
                        spentCoins = Valid_Operations.SelectMany(o => o.SpentCoins).ToList();
                    }

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

        private static List<ICoin> GetUTXOFromInsight(List<ICoin> UTXOs, BitcoinAddress ca, string baseURL)
        {
            List<UTXO> addressUTXOs;
            List<ICoin> unspentCoins = new List<ICoin>();
            var iclient = new RestClient();
            iclient.BaseUrl = new Uri(baseURL);
            var utxoRequest = new RestRequest("/addr/{addr}/utxo", Method.GET);
            utxoRequest.AddUrlSegment("addr", ca);
            utxoRequest.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
            //utxoRequest.RootElement = "UTXOs";
            IRestResponse<List<UTXO>> response = iclient.Execute<List<UTXO>>(utxoRequest);
            //list of unspent transactions
            addressUTXOs = response.Data;
            if (addressUTXOs != null)
            {
                foreach (var utxo in addressUTXOs.GroupBy(u => u.txid).Select(group => group.First()))
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

            return unspentCoins;
        }

        public List<Money> CalculateOutputAmounts_Their_My_Fee(List<ICoin> UTXOs, double myfee_percent, double miner_txfee)
        {
            if (miner_txfee > 0.1)
            {
                miner_txfee = 0.09; //Set maximum fee
            }

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

//Phase 1
                        //---------
                        //All users who transfer Bitcoins from his/her own address to his/her own address between Block 494000 and 
                        //    Block 498777 (11 November 2017 to 12 December 2017 GMT) will be eligible if the transaction meets the following 
                        //    criteria.
                        // The output address(receiving address) must also be listed as one of the input addresses 
                        //    and cannot be a totally new address
                        // The output address(receiving address) must end up with a balance of more than 0.01 BTC
                        //
                        // If you are not sure if the operation is complete, please make two transfers.

                        // UBTC rules sleightly different
                        // 1) account held coins prior to block 498777
                        // 2) user must have conducted an outgoing transaction between 494,000 and fork
                        //Phase 2
                        //---------
                        // 1) not received phase 1
                        // 2) balance at block 498777 larger than 0.01 BTC
                        // 3) user must have conducted an outgoing transaction between had activity (outgoing) between block 502,315 and 507,613 
                        //Phase 2 Grace Period
                        // 3) had activity (outgoing) between block 502,315 and 507,613 

    //List<BalanceOperation> Phase2_ForkOps = AddressTxs.Operations.Where(o => o.Height < 502315 && o.Height > 494000).ToList();

                        //bool inputIsInOutput = false;
                        //if (Phase1_ForkOps.Count() > 0)
                        //{
                        //    foreach (var o in Phase1_ForkOps)
                        //    {
                        //        List<BitcoinAddress> inputAddresses = new List<BitcoinAddress>();
                        //        List<BitcoinAddress> outputAddresses = new List<BitcoinAddress>();

                        //        var t = client.GetTransaction(o.TransactionId).Result;

                        //        foreach (var i in t.Transaction.Outputs)
                        //        {
                        //            var paymentScript = i.ScriptPubKey;
                        //            var address = paymentScript.GetDestinationAddress(Network.Main);
                        //            outputAddresses.Add(address);
                        //            //t.Transaction.Outputs.g
                        //        }
                        //        foreach (var i in t.Transaction.Inputs)
                        //        {
                        //            var inhash = i.PrevOut.Hash;
                        //            var invout = i.PrevOut.N;
                        //            var prevtransaction = client.GetTransaction(inhash).Result.Transaction;
                        //            var sourceAddress = prevtransaction.Outputs[invout].ScriptPubKey.GetDestinationAddress(Network.Main);
                        //            inputAddresses.Add(sourceAddress);
                        //        }
                                
                        //        inputIsInOutput = inputAddresses.Count(i => outputAddresses.Contains(i)) > 0;
                        //        if (inputIsInOutput)
                        //        {
                        //            AutomaticOps.Add(o);
                        //        }
                        //    }