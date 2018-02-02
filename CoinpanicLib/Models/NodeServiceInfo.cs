using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinpanicLib.Models
{
    public class NodeServices
    {
        [Key]
        public int ServiceId { get; set; }
        public string Coin { get; set; }
        public string Endpoint { get; set; }
        public bool IsActive { get; set; }
    }
}
