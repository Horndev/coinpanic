using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using System.Linq;

namespace Coinpanic.Tests
{
    [TestClass]
    public class MultisigTests
    {
        [TestMethod]
        public void Fork_multisig_BTV()
        {
            string addr = "3CgCi4KGK4kx5dddLdQqKHN1nmACMKRrac";
            string priv1 = "LnvAY6k625rPTDrB8jDajWgicFSPoBYZssxWyXucXw7HjvCS3XtA";
            string priv2 = "Ln7dHCpAHCaJ4Y7DjamLH4B3MMozafH4Fa5U9xFsyHRXys1oV1UJ";
            string rds = "5221023009742d61ebe747b295bab6a9268b9dcfbdfccdc0fcb747083068f4be57c6ef21039b1df4cbb3a9f77cc41f7cf807470498f5efd2b1ea46142d16b26fb5af7012e452ae";

            Key a = Key.Parse(priv1);
            Key b = Key.Parse(priv2);

            Script redeemscript = PayToMultiSigTemplate
                .Instance
                .GenerateScriptPubKey(2, new[] {b.PubKey, a.PubKey});

            var ad1 = a.PubKey.GetAddress(Network.Main).ToString();
            var ad2 = b.PubKey.GetAddress(Network.Main).ToString();

            // http://www.soroushjp.com/2014/12/20/bitcoin-multisig-the-hard-way-understanding-raw-multisignature-bitcoin-transactions/
            //How many required (M of N)
            //OpcodeType.OP_2

            var h = new NBitcoin.DataEncoders.HexEncoder();
            Script redeemscript_fb = new Script(h.DecodeData(rds));

            string pubAddress = redeemscript.Hash.GetAddress(Network.Main).ToString();//.GetScriptAddress(Network.Main).ToString();

            Console.WriteLine("Address: " + pubAddress);

            Assert.AreEqual(addr, pubAddress);

            //Add some bitcoin to the multisig address
            var received = new Transaction();
                received.Outputs.Add(new TxOut(Money.Coins(1.0m), redeemscript.Hash));

            Coin coin = received.Outputs.AsCoins().First().ToScriptCoin(redeemscript);

            //Create a transaction requiring two signatories
            BitcoinAddress dest = new Key().PubKey.GetAddress(Network.Main);
            TransactionBuilder builder = new TransactionBuilder();

            Transaction utx =
                builder
                  .AddCoins(coin)
                  .Send(dest, Money.Coins(0.9999m))
                  .SendFees(Money.Coins(0.0001m))
                  .SetChange(redeemscript.Hash)
                  .BuildTransaction(sign: false);

            Console.WriteLine("Unsigned:");
            Console.WriteLine(utx.ToHex());

            Transaction sa =
                builder
                    .AddCoins(coin)
                    .AddKeys(a)
                    .SignTransaction(utx, SigHash.ForkIdBTV, "BTV");

            Console.WriteLine("sa:");
            Console.WriteLine(sa.ToHex());

            Transaction sb =
                builder
                    .AddCoins(coin)
                    .AddKeys(b)
                    .SignTransaction(sa, SigHash.ForkIdBTV, "BTV");

            Console.WriteLine("sb:");
            Console.WriteLine(sb.ToHex());

            Transaction stx = //sb;
                builder
                    .AddCoins(coin)
                    .AddKeys(a)
                    .AddKeys(b)
                    .SignTransaction(utx, SigHash.ForkIdBTV, "BTV");
            //    builder
            //        .AddCoins(coin)
            //        .CombineSignatures("BTV", sa, sb);

            Console.WriteLine("stx:");
            Console.WriteLine(stx.ToHex());

            var res = builder.Verify(stx, "BTV");

            Assert.IsTrue(res);
        }

        [TestMethod]
        public void Fork_multisig_BCA()
        {
            string addr = "3CgCi4KGK4kx5dddLdQqKHN1nmACMKRrac";
            string priv1 = "LnvAY6k625rPTDrB8jDajWgicFSPoBYZssxWyXucXw7HjvCS3XtA";
            string priv2 = "Ln7dHCpAHCaJ4Y7DjamLH4B3MMozafH4Fa5U9xFsyHRXys1oV1UJ";
            string rds = "5221023009742d61ebe747b295bab6a9268b9dcfbdfccdc0fcb747083068f4be57c6ef21039b1df4cbb3a9f77cc41f7cf807470498f5efd2b1ea46142d16b26fb5af7012e452ae";

            Key a = Key.Parse(priv1);
            Key b = Key.Parse(priv2);

            Script redeemscript = PayToMultiSigTemplate
                .Instance
                .GenerateScriptPubKey(2, new[] { b.PubKey, a.PubKey });

            var ad1 = a.PubKey.GetAddress(Network.Main).ToString();
            var ad2 = b.PubKey.GetAddress(Network.Main).ToString();

            // http://www.soroushjp.com/2014/12/20/bitcoin-multisig-the-hard-way-understanding-raw-multisignature-bitcoin-transactions/
            //How many required (M of N)
            //OpcodeType.OP_2

            var h = new NBitcoin.DataEncoders.HexEncoder();
            Script redeemscript_fb = new Script(h.DecodeData(rds));

            string pubAddress = redeemscript.Hash.GetAddress(Network.Main).ToString();//.GetScriptAddress(Network.Main).ToString();

            Console.WriteLine("Address: " + pubAddress);

            Assert.AreEqual(addr, pubAddress);

            //Add some bitcoin to the multisig address
            var received = new Transaction();
            received.Outputs.Add(new TxOut(Money.Coins(1.0m), redeemscript.Hash));

            Coin coin = received.Outputs.AsCoins().First().ToScriptCoin(redeemscript);

            //Create a transaction requiring two signatories
            BitcoinAddress dest = new Key().PubKey.GetAddress(Network.Main);
            TransactionBuilder builder = new TransactionBuilder();

            Transaction utx =
                builder
                  .AddCoins(coin)
                  .Send(dest, Money.Coins(0.9999m))
                  .SendFees(Money.Coins(0.0001m))
                  .SetChange(redeemscript.Hash)
                  .BuildTransaction(sign: false);

            Console.WriteLine("Unsigned:");
            Console.WriteLine(utx.ToHex());

            Transaction sa =
                builder
                    .AddCoins(coin)
                    .AddKeys(a)
                    .SignTransaction(utx, SigHash.ForkIdBTV, "BCA");

            Console.WriteLine("sa:");
            Console.WriteLine(sa.ToHex());

            Transaction sb =
                builder
                    .AddCoins(coin)
                    .AddKeys(b)
                    .SignTransaction(sa, SigHash.ForkIdBTV, "BCA");

            Console.WriteLine("sb:");
            Console.WriteLine(sb.ToHex());

            Transaction stx = //sb;
                builder
                    .AddCoins(coin)
                    .AddKeys(a)
                    .AddKeys(b)
                    .SignTransaction(utx, SigHash.ForkIdBTV, "BCA");
            //    builder
            //        .AddCoins(coin)
            //        .CombineSignatures("BTV", sa, sb);

            Console.WriteLine("stx:");
            Console.WriteLine(stx.ToHex());

            var res = builder.Verify(stx, "BCA");

            Assert.IsTrue(res);
        }
    }
}
