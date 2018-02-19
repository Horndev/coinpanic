using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace coinpanic_airdrop.Models
{
    public class Coupon
    {
        [Key]
        public string CouponId { get; set; }
        public double FeeRate { get; set; }
        public int NumTimesUsed { get; set; }
        public int MaxTimesUsed { get; set; }
    }
}