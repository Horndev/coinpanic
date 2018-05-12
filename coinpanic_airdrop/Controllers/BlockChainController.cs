using CoinController;
using coinpanic_airdrop.Database;
using coinpanic_airdrop.Models;
using NBitcoin;
using NBitcoin.Forks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace coinpanic_airdrop.Controllers
{
    public class BlockChainController : Controller
    {
        // GET: BlockChain
        public ActionResult Index()
        {
            return RedirectToAction("MultiCoin");
        }

        public JsonResult Search(string coin, string address)
        {
            AddressSummary result = new AddressSummary();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult MultiCoin()
        {
            return View();
        }

        /// <summary>
        /// This sets up the multi-coin claims page.  Each coin is another asynchronous call
        /// </summary>
        /// <param name="addresses"></param>
        /// <returns></returns>
        [HttpPost, AllowAnonymous]
        public ActionResult MultiCoinResults(string addresses)
        {
            List<string> validCoins = new List<string>();
            //List<string> knownCoins = BitcoinForks.ForkByShortName.Keys.ToList();
            using (CoinpanicContext db = new CoinpanicContext())
            {
                validCoins = db.IndexCoinInfo.AsNoTracking().Select(i => i.Coin).ToList();
            }
            AddressSearchViewModel viewModel = new AddressSearchViewModel()
            {
                Addresses = addresses.Replace("\r\n",","),
                Coins = validCoins,
            };

            return View(viewModel);
        }

        
        public ActionResult CoinBalance(string coin, string addresses)
        {

            List<string> addressList = new List<string>(
                               addresses.Split(new string[] { "," },
                               StringSplitOptions.RemoveEmptyEntries));

            var invalid = addressList.Where(a => !Bitcoin.IsValidAddress(a)).ToList();

            var addressesToCheck = addressList.Except(invalid).ToList();

            var scanner = new BlockScanner();
            var claimAddresses = Bitcoin.ParseAddresses(addressesToCheck);

            Tuple<List<ICoin>, Dictionary<string, double>> claimcoins;
            bool usedExplorer = false;
            double TotalValue = 0.0 ;
            bool searchError = false;
            Dictionary<string, double> balances;
            try
            {
                claimcoins = scanner.GetUnspentTransactionOutputs(claimAddresses, coin, out usedExplorer);
                TotalValue = Convert.ToDouble(claimcoins.Item1.Sum(o => ((Money)o.Amount).ToDecimal(MoneyUnit.BTC)));
                balances = claimcoins.Item2;
            }
            catch (Exception e)
            {
                balances = new Dictionary<string, double>();
                searchError = true;
            }

            using (CoinpanicContext db = new CoinpanicContext())
            {
                db.IndexCoinInfo.Where(i => i.Coin == coin).ToList();


            }

            AddressSummary result = new AddressSummary()
            {
                InvalidAddresses = invalid,
                CoinName = BitcoinForks.ForkByShortName[coin].LongName,
                Empty = TotalValue <= 0,
                Coin = coin,
                Balance = Convert.ToString(TotalValue),
                UsedExplorer = usedExplorer,
                Addresses = addressesToCheck,
                SearchError = searchError,
            };

            return PartialView(result);
        }
    }
}