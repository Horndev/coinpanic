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

        public static readonly Dictionary<ForkCode, int> ForkBlock = new Dictionary<ForkCode, int>
        {
            { ForkCode.B2X, 501451 },
            { ForkCode.BTF, 500000 },
            { ForkCode.FBTC, 501225 },
            { ForkCode.UBTC, 501878 },
            { ForkCode.SBTC, 498888 },
            { ForkCode.BCX, 498888 },
            { ForkCode.BCD, 495866 },
            { ForkCode.BCH, 478588 },
            { ForkCode.BTG, 491407 },
            { ForkCode.BTW, 499777 },
        };

        public static readonly Dictionary<ForkCode, string> ForkShortName = new Dictionary<ForkCode, string>
        {
            { ForkCode.B2X, "B2X" },
            { ForkCode.BTF, "BTF" },
            { ForkCode.UBTC, "UBTC" },
            { ForkCode.FBTC, "FBTC" },
            { ForkCode.SBTC, "SBTC" },
            { ForkCode.BCX, "BCX" },
            { ForkCode.BCD, "BCD" },
        };

        public static readonly Dictionary<string, ForkCode> ForkShortNameCode = new Dictionary<string, ForkCode>
        {
            {  "B2X",ForkCode.B2X },
            {  "BTF", ForkCode.BTF },
            {  "UBTC",ForkCode.UBTC },
            {  "FBTC",ForkCode.FBTC },
            {  "SBTC",ForkCode.SBTC },
            {  "BCX",ForkCode.BCX },
            {  "BCD",ForkCode.BCD },
        };

        public static readonly Dictionary<ForkCode, string> ForkLongName = new Dictionary<ForkCode, string>
        {
            { ForkCode.B2X, "Segwit2x" },
            { ForkCode.BTF, "Bitcoin Faith" },
            { ForkCode.UBTC, "United Bitcoin" },
            { ForkCode.FBTC, "Fast Bitcoin" },
            { ForkCode.SBTC, "Super Bitcoin" },
            { ForkCode.BCX, "Bitcoin X" },
            { ForkCode.BCD, "Bitcoin Diamond" },
        };

    }
}
