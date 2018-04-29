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
    public class Input
    {
        public double val { get; set; }
        public string address { get; set; }
        public int n { get; set; }
        public string txid { get; set; }
    }

    public class Output
    {
        public double val { get; set; }
        public string address { get; set; }
        public int n { get; set; }
    }

    public class Tx
    {
        public int confirmations { get; set; }
        public double outamount { get; set; }
        public string hash { get; set; }
        public string timeStr { get; set; }
        public int locktime { get; set; }
        public string blockhash { get; set; }
        public int blocktime { get; set; }
        public string io { get; set; }
        public double inamount { get; set; }
        public int vsize { get; set; }
        public int version { get; set; }
        public int size { get; set; }
        public double fee { get; set; }
        public int time { get; set; }
        public int height { get; set; }
        public List<Input> inputs { get; set; }
        public string txid { get; set; }
        public List<Output> outputs { get; set; }
    }

    public class AddrResult
    {
        public double unamount { get; set; }
        public double amount { get; set; }
        public List<Tx> txs { get; set; }
        public int times { get; set; }
        public string hash { get; set; }
        public double receive { get; set; }
    }

    public class UBTCTxResp
    {
        public Tx result { get; set; }
        public bool suc { get; set; }
        public string rd { get; set; }
    }

    public class UBTCResp
    {
        public AddrResult result { get; set; }
        public bool suc { get; set; }
        public string rd { get; set; }
    }

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

        public Tuple<List<ICoin>, Dictionary<string, double>> GetUnspentTransactionOutputs(List<BitcoinAddress> clientAddresses, string forkShortName, bool estimate = false)
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
                if (forkShortName == "B2X" && !isSW && !estimate)
                {
                    string baseURL = "https://explorer.b2x-segwit.io/b2x-insight-api";
                    unspentCoins = GetUTXOFromInsight(UTXOs, ca, baseURL);
                }
                else if (forkShortName == "BCI" && !isSW && !estimate)
                {
                    var addr = ca.Convert(Network.BCI);
                    string baseURL = "https://explorer.bitcoininterest.io/api/";
                    unspentCoins = GetUTXOFromInsight(UTXOs, addr, baseURL);
                }
                else if (forkShortName == "BTCP" && !isSW && !estimate)
                {
                    var addr = ca.Convert(Network.BTCP);
                    string baseURL = "https://explorer.btcprivate.org/api";
                    unspentCoins = GetUTXOFromInsight(UTXOs, addr, baseURL);
                }
                else if (forkShortName == "BTX" && !isSW && !estimate)
                {
                    string baseURL = "http://insight.bitcore.cc/api";
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
                else if (forkShortName == "UBTC" && !isSW)
                {
                    unspentCoins = GetUTXOFromUBTCExplorer(ca.ToString());
                    UTXOs.AddRange(unspentCoins);
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

        public static List<ICoin> GetUTXOFromUBTCExplorer(string addr)
        {
            List<ICoin> unspentCoins;
            var iclient = new RestClient();

            //https://main.ub.com/portals/blockexplore/address/14Z1kt7uUCB8rKwhauXmh5qFdv8cKK9fVj?pageNo=1&pageSize=500
            string baseURL = "https://main.ub.com";
            iclient.BaseUrl = new Uri(baseURL);
            var utxoRequest = new RestRequest("/portals/blockexplore/address/{addr}", Method.GET);
            utxoRequest.AddUrlSegment("addr", addr);
            utxoRequest.AddParameter("pageNo", "1");
            utxoRequest.AddParameter("pageSize", "1000");    //why not?
            utxoRequest.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

            IRestResponse<UBTCResp> response = iclient.Execute<UBTCResp>(utxoRequest);
            UBTCResp ubtctxs = response.Data;

            //Convert to coins
            List<ICoin> UTXOs = new List<ICoin>();

            var receivedTxs = ubtctxs.result.txs.Where(t => t.io == "o").ToList(); //Valid_Operations.SelectMany(o => o.ReceivedCoins).ToList();
            var spentTxs = ubtctxs.result.txs.Where(t => t.io == "i").ToList();
            List<ICoin> recvCoins = new List<ICoin>();
            List<ICoin> spentCoins = new List<ICoin>();

            foreach (var t in receivedTxs)
            {
                var txid = t.txid;
                var txRequest = new RestRequest("/portals/blockexplore/tx/{tx}", Method.GET);
                txRequest.AddUrlSegment("tx", txid);
                txRequest.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

                IRestResponse<UBTCTxResp> txr = iclient.Execute<UBTCTxResp>(txRequest);
                UBTCTxResp tx = txr.Data;

                //find the output of this tx which is our address
                var mytx = tx.result.outputs.Where(o => o.address == addr);

                foreach (var to in mytx)
                {
                    var outPoint = new OutPoint(uint256.Parse(txid), (uint)to.n);
                    var txout = new TxOut(new Money(satoshis: Convert.ToInt64(to.val * 100000000.0)), BitcoinAddress.Create(to.address));
                    ICoin coin = new Coin(fromOutpoint: outPoint, fromTxOut: txout);
                    recvCoins.Add(coin);
                }
            }

            foreach (var t in spentTxs)
            {
                var txid = t.txid;
                var txRequest = new RestRequest("/portals/blockexplore/tx/{tx}", Method.GET);
                txRequest.AddUrlSegment("tx", txid);
                txRequest.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

                IRestResponse<UBTCTxResp> txr = iclient.Execute<UBTCTxResp>(txRequest);
                UBTCTxResp tx = txr.Data;

                //find the output of this tx which is our address
                var mytx = tx.result.inputs.Where(i => i.address == addr).ToList();

                foreach (var ti in mytx)
                {
                    var outPoint = new OutPoint(uint256.Parse(ti.txid), (uint)ti.n);
                    var txout = new TxOut(new Money(satoshis: Convert.ToInt64(ti.val * 100000000.0)), BitcoinAddress.Create(ti.address));
                    ICoin coin = new Coin(fromOutpoint: outPoint, fromTxOut: txout);
                    spentCoins.Add(coin);
                }
            }
            var spentouts = spentCoins.Select(sc => sc.Outpoint.Hash).ToList();
            var newunspentCoins = recvCoins.Where(c => spentouts.Contains(c.Outpoint.Hash) == false).ToList();
            UTXOs.AddRange(newunspentCoins);

            unspentCoins = UTXOs;
            return unspentCoins;
        }

        public static List<ICoin> GetUTXOFromInsight(List<ICoin> UTXOs, BitcoinAddress ca, string baseURL)
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
                        if (utxo.satoshis == 0 && utxo.amount > 0) //bug fix for bitcore
                        {
                            utxo.satoshis = Convert.ToInt64(utxo.amount * 100000000);
                        }
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

        public string GetWitnessText(List<ICoin> UTXOs)
        {
            var str = Serializer.ToString(UTXOs);
            return str;
        }
        
        private QBitNinjaClient client;
    }
}
