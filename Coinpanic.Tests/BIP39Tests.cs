using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using System.Security;

namespace Coinpanic.Tests
{
    [TestClass]
    public class BIP39Tests
    {
        [TestMethod]
        public void TestValidImport()
        {
            //This is a throw-away.  There is nothing here.
            var mnemonic = new Mnemonic("turtle front uncle idea crush write shrug there lottery flower risk shell", Wordlist.English);
            Assert.IsTrue(mnemonic.IsValidChecksum);
        }

        public SecureString GetPassword()
        {
            var pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                    Console.Write("*");
                }
            }
            return pwd;
        }

        [TestMethod]
        public void TestBIP39_Passphrase()
        {
            Assert.Inconclusive();
            string seed24 = "";
            string password = "";
            var mnemonic = new Mnemonic(seed24);
            var masterKey = mnemonic.DeriveExtKey(password);
            string derivationpath = "m/44'/0'/0'/0/0";
            KeyPath kp = KeyPath.Parse(derivationpath);
            ExtKey key = masterKey.Derive(kp);
            var p = key.PrivateKey.GetWif(Network.Main);
            var PubAddr2 = p.PubKey.GetAddress(Network.Main).ToString();

            Console.WriteLine(PubAddr2.ToString());
        }

        [TestMethod]
        public void TestBIP39_BIP32Keys()
        {
            // This is a throwaway.
            string seed12 = "turtle front uncle idea crush write shrug there lottery flower risk shell";
            var mnemonic = new Mnemonic(seed12);
            var masterKey = mnemonic.DeriveExtKey(null); //this is the root key
            int numaccounts = 5;
            int numaddr = 20;
            int coinIx = 0;   //https://github.com/satoshilabs/slips/blob/master/slip-0044.md
            for (int i = 0; i < numaccounts; i++)
            {   //                      bitcoin/account/change/addr
                for (int j = 0; j < numaddr; j++)
                {
                    string derivationpath = "m/49'/" + Convert.ToString(coinIx) + "'/";
                    string fullpath = derivationpath + Convert.ToString(i) + "'/0/" + Convert.ToString(j);
                    KeyPath kp = KeyPath.Parse(fullpath);
                    ExtKey key = masterKey.Derive(kp);
                    //Console.WriteLine("Key " + i + " : " + key.ToString(Network.Main));
                    
                    //Change Addresses
                    //kp = KeyPath.Parse(derivationpath + Convert.ToString(i) + "'/1/" + Convert.ToString(j));
                    //key = masterKey.Derive(kp);
                    //Console.WriteLine("Key " + i + " : " + key.ToString(Network.Main));

                    //generate WIF 
                    var p = key.PrivateKey.GetWif(Network.Main);

                    //bech32 addresses
                    //var privkey = "LB8PaHs9t9UxUPYgGqeJRCZ4KK6YvnnqXbgbanWzb7ctGKfegCS8";
                    //var p = new BitcoinSecret(privkey, Network.Main);

                    //var ba = BitcoinAddress.Create("bc1qsmd3m8sgrhkqp3pefag6n5yahgx3ds226qva07");

                    var PubAddr2 = p.PubKey.GetAddress(Network.Main).ToString();    //normal
                    var PubAddrSW = p.PubKey.GetSegwitAddress(Network.Main);	    //bech32
                    var PubP2SH = PubAddrSW.GetScriptAddress().ToString();          //SH
                    if (PubP2SH.Substring(0, 6) == "34xped")
                    {
                        int q = 1;
                    }
                        
                    Console.WriteLine(PubAddrSW);
                }
            }
            //Assert.IsTrue(mnemonic.IsValidChecksum);
        }
    }
}
