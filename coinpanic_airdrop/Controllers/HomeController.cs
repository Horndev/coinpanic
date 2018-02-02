using coinpanic_airdrop.Database;
using coinpanic_airdrop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace coinpanic_airdrop.Controllers
{
    public class HomeController : Controller
    {
        private CoinpanicContext db = new CoinpanicContext();

        public ActionResult Index()
        {
            IndexModel viewModel = new IndexModel();
            viewModel.CoinInfo = new Dictionary<string, IndexCoinInfo>();

            var ci = db.IndexCoinInfo.ToList();

            foreach (var i in ci)
            {
                var c = new IndexCoinInfo()
                {
                    CoinName = i.CoinName,
                    AlertClass = i.AlertClass,
                    Status = i.Status,
                    CoinHeaderMessage = i.CoinHeaderMessage,
                    Exchange = i.Exchange,
                    ExchangeURL = i.ExchangeURL,
                    ExchangeConfirm = i.ExchangeConfirm,
                    //Nodes = CoinPanicServer.GetNumNodes(i.Coin),
                    CoinNotice = i.CoinNotice,
                };
                viewModel.CoinInfo.Add(i.Coin, c);
            }

            //viewModel.CoinInfo = new Dictionary<string, IndexCoinInfo>()
            //{
            //    { "BTW", new IndexCoinInfo() {
            //        CoinName = "Bitcoin World (BTW)",
            //        AlertClass ="alert-success",
            //        Status = "online",
            //        CoinHeaderMessage = "claims now live.",
            //        Exchange = "btctrade.im",
            //        ExchangeURL = "http://btctrade.im",
            //        ExchangeConfirm = "Confirmed to work by Coinpanic.com.  KYC required.",
            //        Nodes = CoinPanicServer.GetNumNodes("BTW"),
            //        CoinNotice = "Node currently syncing and submitting signed transactions will be available shortly.",
            //    }},
            //};

            return View(viewModel);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Coinpanic Bitcoin Services.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Coinpanic Bitcoin Services.";

            return View();
        }

        public ActionResult Claim()
        {
            return View();
        }

        public ActionResult Legal()
        {
            return View();
        }
    }
}