using coinpanic_airdrop.Models;
using NodeInterface.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace coinpanic_airdrop.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class ServicesController : Controller
    {
        // GET: Services
        public ActionResult Index()
        {
            string apiroot = "https://www.metabittrader.com/";

            string coin = "BTX";

            var iclient = new RestClient();
            iclient.BaseUrl = new Uri(apiroot + coin + "/api/");
            var req = new RestRequest("/peers", Method.GET);
            //req.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
            IRestResponse<List<PeerInfo>> response = iclient.Execute<List<PeerInfo>>(req);

            var peers = response.Data;
            ViewBag.Peers = peers;

            var vm = new ServicesViewModel()
            {
                Services = new List<ForkService>()
                {
                   new ForkService()
                   {
                       Coin = coin,
                       Peers = peers.Select(p => new Peer() { IP = p.ip}).ToList()
                   }
                }
            };
            
            return View(vm);
        }
    }
}