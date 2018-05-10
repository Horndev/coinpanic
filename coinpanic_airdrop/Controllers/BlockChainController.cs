using CoinController;
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

        [HttpPost, AllowAnonymous]
        public ActionResult MultiCoinResults(string addresses)
        {
            AddressSearchViewModel viewModel = new AddressSearchViewModel()
            {
                Addresses = addresses.Replace("\r\n",","),
                Coins = BitcoinForks.ForkByShortName.Keys.ToList(),
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
            var claimcoins = scanner.GetUnspentTransactionOutputs(claimAddresses, coin, out bool usedExplorer);

            var TotalValue = Convert.ToDouble(claimcoins.Item1.Sum(o => ((Money)o.Amount).ToDecimal(MoneyUnit.BTC)));
            var balances = claimcoins.Item2;

            AddressSummary result = new AddressSummary()
            {
                InvalidAddresses = invalid,
                CoinName = BitcoinForks.ForkByShortName[coin].LongName,
                Empty = TotalValue <= 0,
                Coin = coin,
                Balance = Convert.ToString(TotalValue),
                UsedExplorer = usedExplorer,
            };

            return PartialView(result);
        }
    }
}