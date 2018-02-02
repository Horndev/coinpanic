using NBitcoin.Forks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinController
{
    public static class Forks
    {
        public enum ForkCode
        {
            BTC = 0,    //The original chain
            BCH,        //Bitcoin Cash
            SBTC,       //Super Bitcoin
            BCD,        //Bitcoin Diamond
            UBTC,       //United Bitcoin
            BTX,        //Bitcore
            BTW,        //BitcoinWorld
            B2X,        //Segwit2x
            BTF,        //Bitcoin Faith
            FBTC,       //Fast Bitcoin
            BTG,        //Bitcoin Gold
            BCX,        //Bitcoin X
            NUMBER_OF_FORKS     //Leave at end to know how many forks are supported
        }
    }
}
