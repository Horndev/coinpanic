using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;

namespace Coinpanic.Tests
{
    [TestClass]
    public class BIP39Tests
    {
        [TestMethod]
        public void TestValidImport()
        {
            var mnemonic = new Mnemonic("turtle front uncle idea crush write shrug there lottery flower risk shell", Wordlist.English);
            Assert.IsTrue(mnemonic.IsValidChecksum);
        }

        [TestMethod]
        public void TestBIP39_BIP32Keys()
        {
            var mnemonic = new Mnemonic("hero cruel end salad blood report ribbon donkey shoe undo salad cargo", Wordlist.English);

            var masterKey = mnemonic.DeriveExtKey(); //this is the root key
            int numaccounts = 5;
            int numaddr = 20;
            for (int i = 0; i < numaccounts; i++)
            {   //                      bitcoin/account/change/addr
                for (int j = 0; j < numaddr; j++)
                {
                    KeyPath kp = KeyPath.Parse("m/44'/0'/" + Convert.ToString(i) + "'/0/" + Convert.ToString(j));
                    ExtKey key = masterKey.Derive(kp);
                    Console.WriteLine("Key " + i + " : " + key.ToString(Network.Main));
                    //Change Addresses
                    kp = KeyPath.Parse("m/44'/0'/" + Convert.ToString(i) + "'/1/" + Convert.ToString(j));
                    key = masterKey.Derive(kp);
                    Console.WriteLine("Key " + i + " : " + key.ToString(Network.Main));

                    //generate WIF 
                    var p = key.PrivateKey.GetWif(Network.Main);

                    //bech32 addresses
                    //var privkey = "LB8PaHs9t9UxUPYgGqeJRCZ4KK6YvnnqXbgbanWzb7ctGKfegCS8";
                    //var p = new BitcoinSecret(privkey, Network.Main);

                    //var ba = BitcoinAddress.Create("bc1qsmd3m8sgrhkqp3pefag6n5yahgx3ds226qva07");

                    //var PubAddr2 = p.PubKey.GetAddress(Network.Main);
                    var PubAddrSW = p.PubKey.GetSegwitAddress(Network.Main);	//bech32
                    //var PubP2SH = PubAddrSW.GetScriptAddress();                     //SH
                    Console.WriteLine(PubAddrSW);
                }
            }
            //Assert.IsTrue(mnemonic.IsValidChecksum);
        }
    }
}
