using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using NodeInterface.Models;
using NodeInterface.Database;
using CoinpanicLib.NodeConnection;
using System.Diagnostics;
using System.Web.Http.Cors;

namespace NodeInterface.Controllers
{
    /// <summary>
    /// Manages interface to node peer connections.
    /// </summary>
    [RoutePrefix("api/peers")]
    [EnableCors(origins: "*", headers: "*", methods: "*", SupportsCredentials = true)]
    public class PeersController : ApiController
    {
        private CoinpanicContext db = new CoinpanicContext();

        private readonly INodeService nodeService;

        /// <summary>
        /// Dependency Injection Constructor
        /// </summary>
        /// <param name="ns"></param>
        public PeersController(INodeService ns)
        {
            nodeService = ns;
            Debug.Print("Peers Controller created.");
        }

        // GET api/Node/
        /// <summary>
        /// Get the information about connected peers.
        /// </summary>
        /// <returns>Returns information about the connected peers.</returns>
        [Route("")]
        public List<PeerInfo> GetPeers()
        {
            var peers = nodeService.GetConnectedPeers;
            List<PeerInfo> res = new List<PeerInfo>();

            foreach(var p in peers.Nodes)
            {
                PeerInfo np = new PeerInfo()
                {
                    ip = p.IP,
                    port = Convert.ToInt32(p.port),
                };
                res.Add(np);
            }
            //var serviceCoin = System.Configuration.ConfigurationManager.AppSettings["ServiceCoin"];
            //var nodes = db.SeedNodes.Where(n => n.Coin == serviceCoin);
            //var res = nodes.Select(n => new PeerInfo() { ip = n.IP, port = n.Port }).ToList();
            return res;
        }

        /// <summary>
        /// Get the number of connected peers
        /// </summary>
        /// <returns>Number of connected peers</returns>
        [Route("count/")]
        public int GetPeersCount()
        {
            //Debug.Print(nodeService.Coin);
            return nodeService.NumConnectedPeers;
        }


        /// <summary>
        /// [Not Implemented] Try to reconnect the peer
        /// </summary>
        /// <param name="id">The peer identifier</param>
        /// <returns></returns>
        [Route("reconnect/{id}")]
        public PeerInfo Reconnect(int id)
        {
            return new PeerInfo();
        }

        /// <summary>
        /// [Not Implemented] Get information about a connected peer
        /// </summary>
        /// <param name="id">The peer identifier</param>
        /// <returns>Returns information about the connected peer.</returns>
        [Route("{id}")]
        public PeerInfo GetPeer(int id)
        {
            return new PeerInfo();
        }

        /// <summary>
        /// [Not Implemented] Add a new node to peers.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        [Route("add/")]
        [HttpGet]
        public PeerInfo Add(string ip, int port)
        {
            return new PeerInfo();
        }
    }
}
