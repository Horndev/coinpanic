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
        public void TestGetUTXOs()
        {
            string addr = "14Z1kt7uUCB8rKwhauXmh5qFdv8cKK9fVj";
            List<ICoin> unspentCoins = new List<ICoin>();
            unspentCoins = GetUTXOFromUBTCExplorer(addr);

            int z = 1;
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

/*
{"result":
{"unamount":0E-8,
"amount":20.01891265,
"txs":[
    {"confirmations":334,
     "outamount":106685.97802104,
     "hash":"d7d1868363ffe466cf37aad82a8458d99b40393e398c5344eec71e387412dfbf",
     "timeStr":"2018-03-10 03:28:34.0",
     "locktime":505439,
     "blockhash":"00000000000000004a56690ba570b7f825e2e3ab33b692b3ba01d745f6a3411e",
     "blocktime":1520623714,
     "io":"o",
     "inamount":106685.97948271,
     "vsize":67080,
     "version":2,
     "size":67080,
     "fee":0.00146167,
     "time":1520623714,
     "height":505440,
     "inputs":[
        {"val":106685.97948271,"address":"1EYqYVmP9DUP8F9aQH9XzyXCobRZTMtE58","n":1841,"txid":"4334aff5e101d7f8594d4729cdc7abe90831322cfe3a59c07ec6834826b15ca6"}],
     "txid":"d7d1868363ffe466cf37aad82a8458d99b40393e398c5344eec71e387412dfbf",
     "outputs":[
        {"val":20.00000000,"address":"1KQysmZXTBnoWiBzV4ckUKNMXwZYyAwGgn","n":0},
        {"val":20.00000000,"address":"1aCRJcmyiSEc6dksFeeSKjEAFiuEWRHob","n":1},
        {"val":19.99900000,"address":"1FgTzeCZ2kqbbcGiietwxwSV5z1AhNvAgx","n":2},
        {"val":22.17744139,"address":"38rSCvY5Kp7z8S2R5TALUa4PFKbBxPD833","n":3},
        {"val":19.99950000,"address":"1HL864GM2kmrfD1TD19LqgFvRaa77zfTJo","n":4},
        {"val":20.00000466,"address":"1Q15MqB9YHCUT9Fkt31x1hNBmWPXd17Xj6","n":5},
        {"val":20.57019209,"address":"38LUUe3Uhu6pNasSQPrYZWR724EVYrt3qx","n":6},
        {"val":20.53320000,"address":"1FXows8r4RMvyRgsFcxRkxvjP57bH1zfWR","n":7},
        {"val":21.04000000,"address":"1JW7yPFGgqtGu6B9Ftmpi8h99CJ83Mw992","n":8},
        {"val":20.62055807,"address":"1JNRR16SyZxq6KwxP1VGJm22P7rN4LJRYf","n":9}]},
    {"confirmations":6964,
    "outamount":549.86071586,
    "hash":"d17f535ea976a364d9197498e0cb4019425ada0e82153cc5f0a87f3929ab53cb",
    "timeStr":"2017-12-12 03:39:46.0",
    "locktime":0,
    "blockhash":"b1dd86617309e2a3cd1067f4180a9be97e824822fc76e6eb36f6a24e37307786",
    "blocktime":1513021186,
    "io":"i",
    "inamount":549.86311586,
    "vsize":18927,
    "version":2,
    "size":18927,
    "fee":0.00240000,
    "time":1513021186,
    "height":498810,
    "inputs":[
        {"val":0.28000000,"address":"1Q8CHsawkGinAD7QJMYmXupGnHWMU3E1sq","n":1,"txid":"1097f441b21a2bd1fb617657609df8c6eb180c2e2deb05a20adcff52a266591e"},
        {"val":1.42000000,"address":"1BUowmgr6FonjYmMMfuSAjeeK7L7tH191","n":1,"txid":"1926c8e62a5c6dff1f6cc9b782bc44506a1e74d67f36717f813270b81c67591e"},
        {"val":1.69286700,"address":"1NMKjrx14Bq3EszXeFmVVuzgcfuSZjSoEP","n":0,"txid":"d1804160b59630139f1c85fd75e371ec81ecfdb0ea59e05a17e235ea4069591e"},
        {"val":0.04910000,"address":"15J7fJNt8CbcsoRTJqFGYQWeUxAbcbtZJK","n":0,"txid":"1c4c9ad522139c9df6936aeda718684aeb74f1d96935bbf4f63e3f7c656b591e"},
        {"val":20.01891265,"address":"14Z1kt7uUCB8rKwhauXmh5qFdv8cKK9fVj","n":0,"txid":"3a61ed49a53b9b5875199d3312db8d08c7143ba7447d11b57a075aec726b591e"},
        {"val":0.01098111,"address":"1MPCGCH8GyZnMminFeLG7HSQUJHfCfTwsa","n":1,"txid":"0a547b21619301c880753bc76c92cf5426b45acf36403d2c56d1ae3cf96c591e"},
        {"val":0.02230415,"address":"14geEFfPXT9K5Vao9DcoFeikPDJyc7QZUM","n":0,"txid":"659d242761b4e783796c64d5d3045c6cba9d8869efe6de35d6720e8a3b6f591e"},
        {"val":0.16769948,"address":"1BXRksBXfe79ggU8RdVTX9nAzMbjHw4yHn","n":1,"txid":"de90def73fa0ca88d505594d23c07f4d063de54d107033f908785fc59970591e"},
        {"val":0.10000000,"address":"1B2HLRLT7d51gzhcs7gMP1fzB92XBJwnRX","n":1,"txid":"18c66125b7882485352517e62c024e94d9af9789281a48c3abd0f5912471591e"},
        {"val":0.04450346,"address":"1G3k5c98tUs8iFRrCfWx1MbY8ditHkYiDR","n":0,"txid":"a0d39006172bc4c3eee1d11cfa62caa6cfda869e9820c9bd6bfc13f25972591e"}],
    "txid":"d17f535ea976a364d9197498e0cb4019425ada0e82153cc5f0a87f3929ab53cb",
    "outputs":[
        {"val":549.86071586,"address":"31rZdrTpN57Wbfhg7xTPxeFGjEQaMBjxoo","n":0}]},
    {"confirmations":10830,"hash":"3a61ed49a53b9b5875199d3312db8d08c7143ba7447d11b57a075aec726b591e",
    "outamount":20.01891265,
    "timeStr":"2017-11-19 00:02:44.0",
    "locktime":494941,
    "blockhash":"000000000000000000a46c91a952b9f151519995f4bb0050411c8dbe45808179",
    "blocktime":1511020964,
    "io":"o",
    "vsize":583,
    "inamount":20.01909369,
    "version":1,"size":583,"fee":0.00018104,"time":1511020964,"height":494944,
    "inputs":[
        {"val":1.61088000,"address":"1HndfGdrzCX7y3wEu6RRxZTPicYfnkvHzL","n":3,"txid":"a761d30d0176997b3151d1c1a164f0b810e263ab285b0c2a44f8fd8533a026eb"},
        {"val":0.01118000,"address":"1HndfGdrzCX7y3wEu6RRxZTPicYfnkvHzL","n":9,"txid":"2dd8abe59c37b5441b50d2d9fded77103c08dcd27181a5e929d2471c352b25d2"},
        {"val":18.39703369,"address":"1HndfGdrzCX7y3wEu6RRxZTPicYfnkvHzL","n":0,"txid":"1e3c18ca4e665800d75ceabc9fa3206cde41db2a54e03d21547ee0372e006130"}],
    "txid":"3a61ed49a53b9b5875199d3312db8d08c7143ba7447d11b57a075aec726b591e",
    "outputs":[
        {"val":20.01891265,"address":"14Z1kt7uUCB8rKwhauXmh5qFdv8cKK9fVj","n":0}]}],
    "times":3,"hash":"14Z1kt7uUCB8rKwhauXmh5qFdv8cKK9fVj",
    "receive":40.03782530},
"suc":true,
"rd":""}
 */
