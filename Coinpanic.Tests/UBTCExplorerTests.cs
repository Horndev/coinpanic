using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System.Collections.Generic;
using NBitcoin;
using System.Linq;

namespace Coinpanic.Tests
{
    [TestClass]
    public class UBTCExplorerTests
    {
        [TestMethod]
        public void UBTC_Explorer_GetUTXOs()
        {
            string addr = "14Z1kt7uUCB8rKwhauXmh5qFdv8cKK9fVj";
            List<ICoin> unspentCoins = new List<ICoin>();
            unspentCoins = GetUTXOFromUBTCExplorer(addr);
        }

        private static List<ICoin> GetUTXOFromUBTCExplorer(string addr)
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
                    var txout = new TxOut(new Money(satoshis: Convert.ToInt32(to.val * 100000000.0)), BitcoinAddress.Create(to.address));
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
                    var txout = new TxOut(new Money(satoshis: Convert.ToInt32(ti.val * 100000000.0)), BitcoinAddress.Create(ti.address));
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
    }
}

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