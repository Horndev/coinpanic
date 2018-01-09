using coinpanic_airdrop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace coinpanic_airdrop.Controllers
{
    public class ClaimController : Controller
    {
        // GET: Claim

        public ActionResult NewClaim(string coin)
        {
            List<String> ValidCoins = new List<string>()
            {
                "BCD",
                "SBTC",
                "B2X"
            };
            if (ValidCoins.Contains(coin))
            {
                var NewClaim = new CoinClaim { Name = "[COIN NAME]", Code = coin };

                return View(NewClaim);
            }
            else
            {
                return RedirectToAction("InvalidCoin");
            }
            
        }

        public ActionResult InvalidCoin()
        {
            return View();
        }

        public ActionResult BCD()
        {
            return View();
        }

        public ActionResult BCX()
        {
            return View();
        }

        public ActionResult B2X()
        {
            return View();
        }
    }
}