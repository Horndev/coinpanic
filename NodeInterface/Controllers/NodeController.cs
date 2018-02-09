using CoinpanicLib.NodeConnection;
using NodeInterface.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace NodeInterface.Controllers
{
    [RoutePrefix("api/node")]
    [EnableCors(origins: "https://www.coinpanic.com", headers: "*", methods: "*", SupportsCredentials = true)]
    public class NodeController : ApiController
    {
        private CoinpanicContext db = new CoinpanicContext();
        private readonly INodeService nodeService;

        public NodeController(INodeService ns)
        {
            nodeService = ns;
        }

        /// <summary>
        /// Connect to specified node id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("connect/{id}")]
        [HttpGet]
        public bool Connect(string id)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Route("connect/")]
        [HttpGet]
        public bool Connect()
        {
            var seedNodesFromDb = db.SeedNodes.Where(n => n.Coin == nodeService.Coin).ToList();

            var seednodes = seedNodesFromDb.Select(n => new NodeDetails()
            {
                coin = n.Coin,
                ip = n.IP,
                port = n.Port,
                use = n.Enabled,
            }).ToList();

            Debug.Write("Connecting Nodes");

            nodeService.ConnectNodes(seednodes);

            return true;
        }

        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}