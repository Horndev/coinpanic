using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinpanicLib.Models
{
    /// <summary>
    /// Represents the saved result of transactions
    /// </summary>
    public class TxResult
    {
        [Key]
        public int Id { get; set; }

        public string TxId { get; set; }

        public string Coin { get; set; }

        public DateTime? Timestamp { get; set; }

        public string Result { get; set; }

        public string ClaimId { get; set; }

        public string RawTransaction { get; set; }
    }
}
