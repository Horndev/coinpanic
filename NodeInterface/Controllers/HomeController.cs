using CoinpanicLib.NodeConnection;
using NodeInterface.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NodeInterface.Controllers
{
    public class HomeController : Controller
    {

        private readonly INodeService nodeService;

        public HomeController(INodeService ns)
        {
            nodeService = ns;
        }

        public ActionResult Index()
        {
            ViewBag.Title = "Coinpanic Node Interface";
            ViewBag.Coin = nodeService.Coin;
            ViewBag.NumConnectedPeers = nodeService.NumConnectedPeers;


            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Broadcast(BroadcastModel model)
        {
            try
            {
                // Verification  
                if (ModelState.IsValid)
                {
                    // Info.  
                    return this.Json(new
                    {
                        EnableSuccess = true,
                        SuccessTitle = "Success",
                        SuccessMsg = "hello!"
                    });
                }
            }
            catch (Exception ex)
            {
                // Info  
                Console.Write(ex);
            }
            // Info  
            return this.Json(new
            {
                EnableError = true,
                ErrorTitle = "Error",
                ErrorMsg = "Something goes wrong, please try again later"
            });
        }

    }
}
