using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinpanicLib.Models
{
    public class TxSubmitted
    {
        [Key]
        public int txsId { get; set; }
        public string Coin { get; set; }
        public string TransactionHash { get; set; }
        public bool IsError { get; set; }
        public bool IsTransmitted { get; set; }
        public string ResultMessage { get; set; }
        public string ClaimId { get; set; }
        public string SignedTx { get; set; } //copy since user can update ClaimId
    }
}
