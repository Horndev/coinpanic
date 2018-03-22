using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin.Forks;
using CoinController;
using NBitcoin;
using System.Collections.Generic;
using System.Linq;

namespace Coinpanic.Tests
{
    [TestClass]
    public class TransactionTests
    {
        [TestMethod]
        public void TestBPAutxP2PKH()
        {
            string coin = "BPA";
            string fromAddr = "15Lo7GRtK7b8WaYQeynwRFU479FJBSyewr";
            string toAddr = "PPu3XYFn8AhVvXjzrnjBCb5FTim8X3RX8u";
            string expected = "0100000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a914a7eb01a363c1324eb38f04a3b138f8b6cc4ef40188ac00000000";
            double fee = 0.0001;
            string utx = createUnsignedTransaction(coin, fromAddr, toAddr, fee, out string utxos, "BTN");
            Assert.AreEqual(expected, utx);
        }

        [TestMethod]
        public void TestBPAstxP2PKH()
        {
            string coin = "BPA";
            string utxTxt = "0100000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a914a7eb01a363c1324eb38f04a3b138f8b6cc4ef40188ac00000000";
            string ustr = "[\r\n  {\r\n    \"transactionId\": \"3749e4b36586d9ad82b4b02b367c0b008bc0ade1517c901efcdf093ccdefa0bc\",\r\n    \"index\": 8,\r\n    \"value\": 231211,\r\n    \"scriptPubKey\": \"76a9142f9ee53cc09b3136a8c5bcf2b6fac9f81563a87288ac\",\r\n    \"redeemScript\": null\r\n  }\r\n]";
            string privK = "5HsCbiMnMop1z8u4jmjGHziE8ne9R2mNCdizDgvKJiQyeY7EegW";
            string expected = "0100000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e44937080000008b4830450221009bc6fc589f2e097c5b6eb50a77bd27c8ccedc8fab6a18ca561c1e0706e14968f0220395f016b6f7cd27cdc5feee0096ea0cc9b58de1084c28ade1bd35a0c7169c357214104ce9d21fcccea78c02182d7d6e20e87fc7f14920655ca98bc1deebb1bb0945d6a777c187e77cc5e29424a791a07494f457b0c7ebe343dd7cfc56de415a79c5ee9ffffffff011b600300000000001976a914a7eb01a363c1324eb38f04a3b138f8b6cc4ef40188ac00000000";
            string stxStr = SignTransaction(coin, utxTxt, ustr, privK);
            Console.WriteLine(expected);
            Console.WriteLine(stxStr);
            Assert.AreEqual(expected, stxStr);
        }

        [TestMethod]
        public void TestBTPutxP2PKH()
        {
            string coin = "BTP";
            string fromAddr = "15Lo7GRtK7b8WaYQeynwRFU479FJBSyewr";
            string toAddr = "PbQdinKhh9pRKTfkJuhngfX8ctQiZ5cNvy";
            string expected = "0100000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a914262cff1a005936ddc84344eee8469735e06b347688ac00000000";
            double fee = 0.0001;
            string utx = createUnsignedTransaction(coin, fromAddr, toAddr, fee, out string utxos, "BTN");
            Assert.AreEqual(expected, utx);
        }

        [TestMethod]
        public void TestBTPstxP2PKH()
        {
            string coin = "BTP";
            string utxTxt = "0100000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a914262cff1a005936ddc84344eee8469735e06b347688ac00000000";
            string ustr = "[\r\n  {\r\n    \"transactionId\": \"3749e4b36586d9ad82b4b02b367c0b008bc0ade1517c901efcdf093ccdefa0bc\",\r\n    \"index\": 8,\r\n    \"value\": 231211,\r\n    \"scriptPubKey\": \"76a9142f9ee53cc09b3136a8c5bcf2b6fac9f81563a87288ac\",\r\n    \"redeemScript\": null\r\n  }\r\n]";
            string privK = "5HsCbiMnMop1z8u4jmjGHziE8ne9R2mNCdizDgvKJiQyeY7EegW";
            string expected = "0100000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e44937080000008a47304402203844c35376d9dc4a9bfc8cd6463ad27992d17d1885517bcdd70f644eed9e15e00220697d7dc9943fff58b5af6b4c95e6e65340b45b82193d81c3e6dfd60d9f2bfe65414104ce9d21fcccea78c02182d7d6e20e87fc7f14920655ca98bc1deebb1bb0945d6a777c187e77cc5e29424a791a07494f457b0c7ebe343dd7cfc56de415a79c5ee9ffffffff011b600300000000001976a914262cff1a005936ddc84344eee8469735e06b347688ac00000000";
            string stxStr = SignTransaction(coin, utxTxt, ustr, privK);
            Console.WriteLine(expected);
            Console.WriteLine(stxStr);
            Assert.AreEqual(expected, stxStr);
        }

        [TestMethod]
        public void TestBTVutxP2PKH()
        {
            string coin = "BTV";
            string fromAddr = "15Lo7GRtK7b8WaYQeynwRFU479FJBSyewr";
            string toAddr = "1FMjcQ1U1eUKPKpnSpM6ksTrB8sUBefokz";
            string expected = "0200000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";//"0200000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            double fee = 0.0001;
            // This address doesn't have BTV coins, so I use BTN to generate UTX to build a transaction.
            string utx = createUnsignedTransaction(coin, fromAddr, toAddr, fee, out string utxos, "BTN");
            Assert.AreEqual(expected, utx);
        }

        [TestMethod]
        public void TestLBTCutxP2PKH()
        {
            string coin = "LBTC";
            string fromAddr = "15Lo7GRtK7b8WaYQeynwRFU479FJBSyewr";
            string toAddr = "1ByLm6RP1L21y2n8HnJUKSWskJKDGUqSCP";
            string expected = "01ff000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a91478590bf51df05196a387b8e9635708b07e233cc988ac00000000";
            double fee = 0.0001;
            string utx = createUnsignedTransaction(coin, fromAddr, toAddr, fee, out string utxos, "LBTC");
            Assert.AreEqual(expected, utx);
        }

        [TestMethod]
        public void TestBTXutxP2PKH()
        {
            string coin = "BTX";
            string fromAddr = "15Lo7GRtK7b8WaYQeynwRFU479FJBSyewr";
            string toAddr = "1FMjcQ1U1eUKPKpnSpM6ksTrB8sUBefokz";
            string expected = "0200000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            double fee = 0.0001;
            string utx = createUnsignedTransaction(coin, fromAddr, toAddr, fee, out string utxos, "LBTC");
            Assert.AreEqual(expected, utx);
        }

        [TestMethod]
        public void TestBTVutxP2SH_P2WPKH()
        {
            string coin = "BTV";
            string fromAddr = "34ZuYSNSCm5Vtgtfn7PnxKYXP2rbp4N4rC";
            string toAddr = "1FMjcQ1U1eUKPKpnSpM6ksTrB8sUBefokz";
            string expected = "02000000065c9be01d9de24f668a8be964e4aa2f544690f5e73dd5b818d5812d69eb98227a0100000000ffffffffd7dc5c66aac944659dc11e7b68282af4fb5182451fff31a320e253d30908d59d0000000000ffffffff3bbe46f65214917b52b18370e0bb9ddaf134f6ee87304bba1e8bba48efd21aa50000000000ffffffff1c62addcda85a37dc9ef8c50fa92c391949d4d97bd17dded24a95dd898cdf64d2c00000000ffffffff8c64c798031f1f70d0e68be210887a95173c528a203fc63f620c59db2ed2d3980000000000ffffffff049978513769443705e355151f8d7d1ae821ae90aa150fa58456bb8047bd387a0600000000ffffffff01771af301000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            double fee = 0.0001;
            string utx = createUnsignedTransaction(coin, fromAddr, toAddr, fee, out string utxos, "BTN");
            Assert.AreEqual(expected, utx);
        }

        [TestMethod]
        public void TestBTVutxP2SH_P2WPKH2()
        {
            string coin = "BTV";
            string fromAddr = "39atb9VMmV9UULHyjtYjakTf1wQ7RZiM2b";
            string toAddr = "1FMjcQ1U1eUKPKpnSpM6ksTrB8sUBefokz";
            string expected = "02000000013617cb17192501c8a4fcef74ec360930f05b5bc3f8bf128d9904d5056111bc7a0000000000ffffffff01104d4701000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            double fee = 0.001;
            string utx = createUnsignedTransaction(coin, fromAddr, toAddr, fee, out string utxos);
             //0200000007c9b995c8828868a800395f85733d2c72673a822dd889b4b603992f7a7edb8a9d0100000000ffffffff52e4552da0378b19a0c57ebc99637fc47bd21bbe5b4f4fa1bac82218d99fef3e0100000000ffffffffa06065e3d8767a3a143638bfc268603b7d1e27e9d3fb903b2b04a2b62e4326030100000000ffffffffbf5a3c37a0608d37e5ab490806f7b47fcc2a2f8fea0ed9ae2477214441cd722c0000000000ffffffff2e8bffbbc8883f270df425f1393ec91935681eb28317876dff99f34ad93632380000000000ffffffff1f08c14e5eb5d64d2b5ede3faf4cb43032c87355438eca81a38db0dc0afb6c320000000000fffffffff9fddefa48971fa2fa6a46c5a990640cd164351076c004662f8c476f8cdc75f90000000000ffffffff01cec3e003000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000
            Assert.AreEqual(expected, utx);
        }

        [TestMethod]
        public void TestBTXstxP2PKH()
        {
            string coin = "BTX";
            string utxTxt = "0200000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            string ustr = "[\r\n  {\r\n    \"transactionId\": \"3749e4b36586d9ad82b4b02b367c0b008bc0ade1517c901efcdf093ccdefa0bc\",\r\n    \"index\": 8,\r\n    \"value\": 231211,\r\n    \"scriptPubKey\": \"76a9142f9ee53cc09b3136a8c5bcf2b6fac9f81563a87288ac\",\r\n    \"redeemScript\": null\r\n  }\r\n]";
            string privK = "5HsCbiMnMop1z8u4jmjGHziE8ne9R2mNCdizDgvKJiQyeY7EegW";
            string expected = "0200000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e44937080000008b48304502210082f08b0d6c5550f6241d02c1f3368d786fef1ef4a7b90f63bee252d9cef7c47e02201cabc773ec12969c80bd85cbe515c8b4ca932820cc4dbb647c48e960afe782c6014104ce9d21fcccea78c02182d7d6e20e87fc7f14920655ca98bc1deebb1bb0945d6a777c187e77cc5e29424a791a07494f457b0c7ebe343dd7cfc56de415a79c5ee9ffffffff011b600300000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            string stxStr = SignTransaction(coin, utxTxt, ustr, privK);
            Console.WriteLine(expected);
            Console.WriteLine(stxStr);
            Assert.AreEqual(expected, stxStr);
        }

        [TestMethod]
        public void TestLBTCstxP2PKH()
        {
            string coin = "LBTC";
            string utxTxt = "01ff000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a91478590bf51df05196a387b8e9635708b07e233cc988ac00000000";
            string ustr = "[\r\n  {\r\n    \"transactionId\": \"3749e4b36586d9ad82b4b02b367c0b008bc0ade1517c901efcdf093ccdefa0bc\",\r\n    \"index\": 8,\r\n    \"value\": 231211,\r\n    \"scriptPubKey\": \"76a9142f9ee53cc09b3136a8c5bcf2b6fac9f81563a87288ac\",\r\n    \"redeemScript\": null\r\n  }\r\n]";
            string privK = "5HsCbiMnMop1z8u4jmjGHziE8ne9R2mNCdizDgvKJiQyeY7EegW";
            string expected = "01ff000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e44937080000008b48304502210087df28962a06b9b04ade42b12ae24afe14e8e4d62db3bda6669bc6a52169775c02203e8aa77f283c95b38aa92bcb89b1d28c2663ce6e8667d85f68011e5cd58c546f014104ce9d21fcccea78c02182d7d6e20e87fc7f14920655ca98bc1deebb1bb0945d6a777c187e77cc5e29424a791a07494f457b0c7ebe343dd7cfc56de415a79c5ee9ffffffff011b600300000000001976a91478590bf51df05196a387b8e9635708b07e233cc988ac00000000";
            string stxStr = SignTransaction(coin, utxTxt, ustr, privK);
            Console.WriteLine(expected);
            Console.WriteLine(stxStr);
            Assert.AreEqual(expected, stxStr);
        }

        [TestMethod]
        public void TestBTVstxP2SH_P2WPKH2()
        {
            string coin = "BTV";
            string utxTxt = "02000000013617cb17192501c8a4fcef74ec360930f05b5bc3f8bf128d9904d5056111bc7a0000000000ffffffff01104d4701000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            string ustr = "[\r\n  {\r\n    \"transactionId\": \"7abc116105d504998d12bff8c35b5bf0300936ec74effca4c801251917cb1736\",\r\n    \"index\": 0,\r\n    \"value\": 21550000,\r\n    \"scriptPubKey\": \"a91456986355fded8640265c376c17e2d085a38a202a87\",\r\n    \"redeemScript\": null\r\n  }\r\n]";
            string privK = "KyvBqbTV5KMZWtxeJp9DHku42AesQic6tdijFYnFTKsodWYvWb6Q";
            string expected = "020000000001013617cb17192501c8a4fcef74ec360930f05b5bc3f8bf128d9904d5056111bc7a00000000171600147d1d3783aad63c4979518725d42c36b2659595eeffffffff01104d4701000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac0247304402207e9553f39aeaaefcbb87cfbbb0f9e8b7f45997ca62707bfe584c36008dcfacb60220320ca5505547817398446400410881fe94bd0ba05bbacf63bdcd7dc059adb37e652103048d56cddd8c870eab06cd3a064817658fd59db4789211190e00ef947b31011a00000000";
            string stxStr = SignTransaction(coin, utxTxt, ustr, privK);
            //txid: "7551e2a105945e5304c106f2dbae0ff46c097a58213cd0bdd29fad1432d329cf"
            Assert.AreEqual(expected, stxStr);
        }

        [TestMethod]
        public void TestBTVstxP2PKH()
        {
            string coin = "BTV";
            string utxTxt = "0200000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e449370800000000ffffffff011b600300000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            string ustr = "[\r\n  {\r\n    \"transactionId\": \"3749e4b36586d9ad82b4b02b367c0b008bc0ade1517c901efcdf093ccdefa0bc\",\r\n    \"index\": 8,\r\n    \"value\": 231211,\r\n    \"scriptPubKey\": \"76a9142f9ee53cc09b3136a8c5bcf2b6fac9f81563a87288ac\",\r\n    \"redeemScript\": null\r\n  }\r\n]";
            string privK = "5HsCbiMnMop1z8u4jmjGHziE8ne9R2mNCdizDgvKJiQyeY7EegW";
            string expected =   "0200000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e44937080000008b483045022100bb8aad93c23ed609f3c734aefff15498a41b2f1cfce381c2087f5ac2968801ce02204a53c3495e4f6cb62421cd982a8819b46a38f26921da3508cdeb1b7c3be0989d414104ce9d21fcccea78c02182d7d6e20e87fc7f14920655ca98bc1deebb1bb0945d6a777c187e77cc5e29424a791a07494f457b0c7ebe343dd7cfc56de415a79c5ee9ffffffff011b600300000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            //string expected = "0200000001bca0efcd3c09dffc1e907c51e1adc08b000b7c362bb0b482add98665b3e44937080000008b483045022100bb8aad93c23ed609f3c734aefff15498a41b2f1cfce381c2087f5ac2968801ce02204a53c3495e4f6cb62421cd982a8819b46a38f26921da3508cdeb1b7c3be0989d414104ce9d21fcccea78c02182d7d6e20e87fc7f14920655ca98bc1deebb1bb0945d6a777c187e77cc5e29424a791a07494f457b0c7ebe343dd7cfc56de415a79c5ee9ffffffff011b600300000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            string stxStr = SignTransaction(coin, utxTxt, ustr, privK);
            Console.WriteLine(expected);
            Console.WriteLine(stxStr);
            Assert.AreEqual(expected, stxStr);
        }

        [TestMethod]
        public void TestPrivKeyUncompToComp()
        {
            var n = BitcoinForks.ForkByShortName["BTV"].Network; //can use any network which supports segwit
            //This is a Segwit P2SH-P2WPKH address
            string addr = "39atb9VMmV9UULHyjtYjakTf1wQ7RZiM2b";
            //string addr = "14Z1kt7uUCB8rKwhauXmh5qFdv8cKK9fVj";
            var ba = BitcoinAddress.Create(addr, n);
            Assert.IsTrue(ba is BitcoinScriptAddress);

            //This is the "wrong" private key - it is uncompressed
            string privK = "5JRjnSw2q6yKF2MJy9R4h3hLiVvSU8AK6oqRXV3YpeorY2SSP3m";
            //string privK = "5J48ML87SFb8hGWJPLbCZYDfkMk3WZ8cgMbTMLCztiNXdWb9sPn";
            string privKWIF = "KyvBqbTV5KMZWtxeJp9DHku42AesQic6tdijFYnFTKsodWYvWb6Q";
            var clientprivkey = new BitcoinSecret(privK, n);
            var clientprivcompressed = clientprivkey.Copy(compressed:true);
            string WIFKey = clientprivcompressed.ToWif();
            Assert.AreEqual(privKWIF, WIFKey);
        }

        [TestMethod]
        public void TestBTVstxP2SH_P2WPKH()
        {
            string coin = "BTV";
            string utxTxt = "02000000065c9be01d9de24f668a8be964e4aa2f544690f5e73dd5b818d5812d69eb98227a0100000000ffffffffd7dc5c66aac944659dc11e7b68282af4fb5182451fff31a320e253d30908d59d0000000000ffffffff3bbe46f65214917b52b18370e0bb9ddaf134f6ee87304bba1e8bba48efd21aa50000000000ffffffff1c62addcda85a37dc9ef8c50fa92c391949d4d97bd17dded24a95dd898cdf64d2c00000000ffffffff8c64c798031f1f70d0e68be210887a95173c528a203fc63f620c59db2ed2d3980000000000ffffffff049978513769443705e355151f8d7d1ae821ae90aa150fa58456bb8047bd387a0600000000ffffffff01771af301000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac00000000";
            string ustr = "[\r\n  {\r\n    \"transactionId\": \"98d3d22edb590c623fc63f208a523c17957a8810e28be6d0701f1f0398c7648c\",\r\n    \"index\": 0,\r\n    \"value\": 1311156,\r\n    \"scriptPubKey\": \"a9141f9023904096e4dfa8c6dccf065774effe93feb787\",\r\n    \"redeemScript\": null\r\n  },\r\n  {\r\n    \"transactionId\": \"9dd50809d353e220a331ff1f458251fbf42a28687b1ec19d6544c9aa665cdcd7\",\r\n    \"index\": 0,\r\n    \"value\": 907079,\r\n    \"scriptPubKey\": \"a9141f9023904096e4dfa8c6dccf065774effe93feb787\",\r\n    \"redeemScript\": null\r\n  },\r\n  {\r\n    \"transactionId\": \"4df6cd98d85da924eddd17bd974d9d9491c392fa508cefc97da385dadcad621c\",\r\n    \"index\": 44,\r\n    \"value\": 942282,\r\n    \"scriptPubKey\": \"a9141f9023904096e4dfa8c6dccf065774effe93feb787\",\r\n    \"redeemScript\": null\r\n  },\r\n  {\r\n    \"transactionId\": \"7a2298eb692d81d518b8d53de7f59046542faae464e98b8a664fe29d1de09b5c\",\r\n    \"index\": 1,\r\n    \"value\": 65351,\r\n    \"scriptPubKey\": \"a9141f9023904096e4dfa8c6dccf065774effe93feb787\",\r\n    \"redeemScript\": null\r\n  },\r\n  {\r\n    \"transactionId\": \"a51ad2ef48ba8b1eba4b3087eef634f1da9dbbe07083b1527b911452f646be3b\",\r\n    \"index\": 0,\r\n    \"value\": 938833,\r\n    \"scriptPubKey\": \"a9141f9023904096e4dfa8c6dccf065774effe93feb787\",\r\n    \"redeemScript\": null\r\n  },\r\n  {\r\n    \"transactionId\": \"7a38bd4780bb5684a50f15aa90ae21e81a7d8d1f1555e3053744693751789904\",\r\n    \"index\": 6,\r\n    \"value\": 28554538,\r\n    \"scriptPubKey\": \"a9141f9023904096e4dfa8c6dccf065774effe93feb787\",\r\n    \"redeemScript\": null\r\n  }\r\n]";
            string privK = "L14bJHXqgyzN3QyJoapwjYDonSkdYTFmbtSBCU52hE5yxfpcL5uP";
            string expected = "020000000001065c9be01d9de24f668a8be964e4aa2f544690f5e73dd5b818d5812d69eb98227a01000000171600149ded6b069178ce328307b5058596129598e4a71cffffffffd7dc5c66aac944659dc11e7b68282af4fb5182451fff31a320e253d30908d59d00000000171600149ded6b069178ce328307b5058596129598e4a71cffffffff3bbe46f65214917b52b18370e0bb9ddaf134f6ee87304bba1e8bba48efd21aa500000000171600149ded6b069178ce328307b5058596129598e4a71cffffffff1c62addcda85a37dc9ef8c50fa92c391949d4d97bd17dded24a95dd898cdf64d2c000000171600149ded6b069178ce328307b5058596129598e4a71cffffffff8c64c798031f1f70d0e68be210887a95173c528a203fc63f620c59db2ed2d39800000000171600149ded6b069178ce328307b5058596129598e4a71cffffffff049978513769443705e355151f8d7d1ae821ae90aa150fa58456bb8047bd387a06000000171600149ded6b069178ce328307b5058596129598e4a71cffffffff01771af301000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac024830450221008dcd335fbecab4afe44b6d9c800774edd7ff69b3f33ced8f3094f672a8167a3d02206167ebd4fe9e26e7e10ea4de4fb3af81665aeab0bc5f2448ef9d3ff93b45465965210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf260247304402206d10447dee2869b5cecd28e644b5ce4cd339645de591e1b602154cb4c9c10369022060144eacb2472a684de6f9d2df21a2b9433d757b34ece57dc912720794aa2b4465210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf2602483045022100c03e0ba135423bc29255be95acb87ee86881637dfbaa8e54dcc4c0a052f6d35a02200a01bb157cc12034ec50b1e677dec8a31f8ebaf3d214f9875ee779b6c3e5289a65210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf260247304402205071eee74067c5fb72bcc341b883d4a44cabb879497c13004e064601de84245502200bcd734761370cda3643722c575e3a0712ec1fbb065071da346405f964d005fc65210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf260247304402200abe098fc04d9e9d0426f429017dbb6ccc2e81d825884e59836d3fd70b3a391b022016b49f8e739f2a64e82893669b778918ed7c3bcf7c53f5cc4d679df676adbe8965210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf26024830450221008616d43a030403ccee61301a5a32608d637c43506a9c5cb03527315baabea0ed02204dcca1006ee9f2baad27801a04d201b60643e7fe192f8b56d738122f6499639165210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf2600000000";//"020000000001065c9be01d9de24f668a8be964e4aa2f544690f5e73dd5b818d5812d69eb98227a01000000171600149ded6b069178ce328307b5058596129598e4a71cffffffffd7dc5c66aac944659dc11e7b68282af4fb5182451fff31a320e253d30908d59d00000000171600149ded6b069178ce328307b5058596129598e4a71cffffffff3bbe46f65214917b52b18370e0bb9ddaf134f6ee87304bba1e8bba48efd21aa500000000171600149ded6b069178ce328307b5058596129598e4a71cffffffff1c62addcda85a37dc9ef8c50fa92c391949d4d97bd17dded24a95dd898cdf64d2c000000171600149ded6b069178ce328307b5058596129598e4a71cffffffff8c64c798031f1f70d0e68be210887a95173c528a203fc63f620c59db2ed2d39800000000171600149ded6b069178ce328307b5058596129598e4a71cffffffff049978513769443705e355151f8d7d1ae821ae90aa150fa58456bb8047bd387a06000000171600149ded6b069178ce328307b5058596129598e4a71cffffffff01771af301000000001976a9149d7da644b0db0d97a23e0c8a64fa644d2079ef5288ac024830450221008dcd335fbecab4afe44b6d9c800774edd7ff69b3f33ced8f3094f672a8167a3d02206167ebd4fe9e26e7e10ea4de4fb3af81665aeab0bc5f2448ef9d3ff93b45465965210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf260247304402206d10447dee2869b5cecd28e644b5ce4cd339645de591e1b602154cb4c9c10369022060144eacb2472a684de6f9d2df21a2b9433d757b34ece57dc912720794aa2b4465210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf2602483045022100c03e0ba135423bc29255be95acb87ee86881637dfbaa8e54dcc4c0a052f6d35a02200a01bb157cc12034ec50b1e677dec8a31f8ebaf3d214f9875ee779b6c3e5289a65210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf260247304402205071eee74067c5fb72bcc341b883d4a44cabb879497c13004e064601de84245502200bcd734761370cda3643722c575e3a0712ec1fbb065071da346405f964d005fc65210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf260247304402200abe098fc04d9e9d0426f429017dbb6ccc2e81d825884e59836d3fd70b3a391b022016b49f8e739f2a64e82893669b778918ed7c3bcf7c53f5cc4d679df676adbe8965210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf26024830450221008616d43a030403ccee61301a5a32608d637c43506a9c5cb03527315baabea0ed02204dcca1006ee9f2baad27801a04d201b60643e7fe192f8b56d738122f6499639165210319e62a0deea0ede90196204dce7e8462180da6d3bc4a99c84414503efaffbf2600000000"
            string stxStr = SignTransaction(coin, utxTxt, ustr, privK);
            Assert.AreEqual(expected, stxStr);
        }

        private static string createUnsignedTransaction(string coin, string fromAddr, string toAddr, double fee, out string utxos, string ForceCoin = "")
        {
            var n = BitcoinForks.ForkByShortName[coin].Network;
            var destination = BitcoinAddress.Create(toAddr, n);
            var origin = BitcoinAddress.Create(fromAddr);
            Console.WriteLine("From:         " + fromAddr);
            Console.WriteLine("To:           " + toAddr);

            BlockScanner bsc = new BlockScanner();
            // NOTE that BTN is used since this address doesn't actually have BTV
            var balances = bsc.GetUnspentTransactionOutputs(new List<BitcoinAddress>() { origin }, ForceCoin != "" ? ForceCoin : coin);
            //Unspent transaction outputs
            var autxos = balances.Item1;
            var inputAmount = autxos.Sum(u => ((Money)u.Amount).ToDecimal(MoneyUnit.BTC));
            Console.WriteLine("Input Amount: " + Convert.ToString(inputAmount));
            decimal txfeeAmount = Convert.ToDecimal(fee);
            var transferAmount = Money.Coins(Convert.ToDecimal(inputAmount - txfeeAmount));
            var txfeesAmount = Money.Coins(Convert.ToDecimal(txfeeAmount));
            var bcdbuilder = new TransactionBuilder();
            Transaction BCDtx = bcdbuilder
                .AddCoins(coins: autxos)
                .Send(destination, transferAmount)
                .SetChange(destination)
                .SendFees(txfeesAmount)
                .BuildTransaction(sign: false);
            BCDtx.Version = BitcoinForks.ForkByShortName[coin].TransactionVersion;
            string utx = BCDtx.ToHex();
            var ustr = NBitcoin.JsonConverters.Serializer.ToString(autxos);
            Console.WriteLine("UTX: " + utx);
            utxos = ustr;
            return utx;
        }

        [TestMethod]
        public void TestPK()
        {
            string p = "KwQeegf86fW7iARDEtcW9DxadELHVyXdFTSmA8BDnPSMWLjbEqCb";
            var clientprivkey = new BitcoinSecret(p, Network.Main);
            var addr = clientprivkey.PubKey.GetAddress(Network.Main).ToString();    //normal
            Console.WriteLine(addr);
            Assert.AreEqual("13kPnFYM2uDqk8553rycyyd6MMxy569N9e",addr);
        }

        private static string SignTransaction(string coin, string utxTxt, string ustr, string privK)
        {
            var n = BitcoinForks.ForkByShortName[coin].Network;
            var autxos = NBitcoin.JsonConverters.Serializer.ToObject<List<ICoin>>(ustr);
            var utx = Transaction.Parse(utxTxt);
            List<BitcoinSecret> privatekeys = new List<BitcoinSecret>();
            var clientprivkey = new BitcoinSecret(privK, n);
            privatekeys.Add(clientprivkey);
            var sbcdbuilder = new TransactionBuilder();
            utx.sigtype = coin;

            var stx = sbcdbuilder
                    .AddCoins(coins: autxos)
                    //.AddKeys(privatekeys.ToArray())
                    .SignTransaction(
                        utx,
                        BitcoinForks.ForkByShortName[coin].SigHash | SigHash.All,
                        coin);
            stx.Version = BitcoinForks.ForkByShortName[coin].TransactionVersion;

            bool isok = sbcdbuilder.Verify(stx, coin);

            Assert.IsFalse(isok);

            sbcdbuilder = new TransactionBuilder();
            utx.sigtype = coin;
            stx = sbcdbuilder
                .AddKeys(privatekeys.ToArray())
                .AddCoins(coins: autxos)
                .SignTransaction(
                        utx,
                        BitcoinForks.ForkByShortName[coin].SigHash | SigHash.All,
                        coin);

            isok = sbcdbuilder.Verify(stx, coin);
            Assert.IsTrue(isok);

            Console.WriteLine("Signed Transaction:");
            Console.WriteLine(stx.ToHex());
            string stxStr = stx.ToHex();
            return stxStr;
        }

        [TestMethod]
        public void TestBTPWithBIP39()
        {
            string fromAddr = "123qWRaufkCnUfh7WMAmJpKdFDw6zBQkn9";
            string seed = "effort arena code feature pretty busy first end report book novel bless system energy silver right donor depth person spend spring industry wolf patrol";
            string passphrase = "Bdcg$lns$138064";
            string derivationPath = "m/44'/0'/0";
            string toAddr = "PPu3XYFn8AhVvXjzrnjBCb5FTim8X3RX8u";
            string coin = "BTP";

            //generate UTX
            double fee = 0.0001;
            string utxos;
            string utxTxt = createUnsignedTransaction(coin, fromAddr, toAddr, fee, out utxos, coin);
            var autxos = NBitcoin.JsonConverters.Serializer.ToObject<List<ICoin>>(utxos);
            var utx = Transaction.Parse(utxTxt);
            utx.sigtype = coin;

            //Sign with seed
            int account = Convert.ToInt32(derivationPath.Split('/').Last());
            var builder = new TransactionBuilder();
            var stx = builder
                .AddCoins(coins: autxos)
                .SignTransaction(
                        utx,
                        BitcoinForks.ForkByShortName[coin].SigHash | SigHash.All,
                        coin);
            
            for (int i = 0; i < 1000; ++i)
            {
                List<BitcoinSecret> privatekeys = new List<BitcoinSecret>()
                {
                    HDPrivKey(seed, account, i, passphrase, change: false, segwit: false, bech32: false),
                    HDPrivKey(seed, account, i, passphrase, change: true,  segwit: false, bech32: false),
                    HDPrivKey(seed, account, i, passphrase, change: false, segwit: true,  bech32: false),
                    HDPrivKey(seed, account, i, passphrase, change: true,  segwit: true,  bech32: false),
                    HDPrivKey(seed, account, i, passphrase, change: false, segwit: false, bech32: true),
                    HDPrivKey(seed, account, i, passphrase, change: true,  segwit: false, bech32: true),
                };

                stx = builder
                    .AddKeys(privatekeys.ToArray())
                    .SignTransaction(
                        stx,
                        BitcoinForks.ForkByShortName[coin].SigHash | SigHash.All,
                        coin);
                if (builder.Verify(stx, coin))
                {
                    break;
                }
            }

            Assert.IsTrue(builder.Verify(stx, coin));
        }

        [TestMethod]
        public void TestSignWithBIP39()
        {
            string seed = "hero cruel end salad blood report ribbon donkey shoe undo salad cargo";
            var addr = HDAddress(seed, 0, 0, change: false);
        }

        private string dpath(bool segwit = false, bool bech32 = false, int coinIx = 0)
        {
            string derivationpath = "m/" + ((bech32 || segwit) ? "49" : "44") 
                + "'/" + Convert.ToString(coinIx) + "'/";
            return derivationpath;
        }

        private BitcoinSecret HDPrivKey(string seed, int account, int n, string passphrase = null, bool change = false, bool segwit = false, bool bech32 = false)
        {
            var mnemonic = new Mnemonic(seed, Wordlist.English);
            var masterKey = mnemonic.DeriveExtKey(passphrase); //this is the root key
            string derivationpath = dpath(segwit, bech32);
            KeyPath kp = KeyPath.Parse(derivationpath + Convert.ToString(account) + "'/" + (change ? "1" : "0") + "/" + Convert.ToString(n));
            ExtKey key = masterKey.Derive(kp);
            var p = key.PrivateKey.GetWif(Network.Main);
            return p;
        }

        private BitcoinAddress HDAddress(string seed, int account, int n, string passphrase = null, bool change = false, bool segwit = false, bool bech32 = false)
        {
            var p = HDPrivKey(seed, account, n, passphrase, change, segwit, bech32);

            BitcoinAddress res;
            if (bech32)
            {
                res = p.PubKey.GetSegwitAddress(Network.Main);        //bech32
            }
            else if (segwit)
            {
                res = p.PubKey.GetSegwitAddress(Network.Main).GetScriptAddress();          //SH
            }
            else
            {
                res = p.PubKey.GetAddress(Network.Main);
            }
            return res;
        }
    }
}
