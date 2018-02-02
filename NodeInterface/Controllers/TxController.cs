using CoinpanicLib.Models;
using CoinpanicLib.NodeConnection;
using NodeInterface.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NodeInterface.Controllers
{
    /// <summary>
    /// Transactions Controller
    /// </summary>
    [RoutePrefix("api/tx")]
    public class TxController : ApiController
    {
        private readonly INodeService nodeService;

        public TxController(INodeService ns)
        {
            nodeService = ns;
            Debug.Print("Tx Controller created.");
        }

        /// <summary>
        /// Transmits a transaction to the network.
        /// </summary>
        /// <param name="hex">The raw transaction in hexadecimal format.</param>
        [Route("broadcast/")]
        [HttpPost]
        public void Post([FromBody]string hex)
        {

        }

        /// <summary>
        /// Gets the status of a transaction
        /// </summary>
        /// <param name="txid">The transaction id.</param>
        /// <returns>Returns information about the requested transaction, or null if unknown.</returns>
        [Route("{txid}")]
        public TxInfo Get(string txid)
        {
            nodeService.Test();
            nodeService.Val = txid;
            return new TxInfo();
        }
    }
}
