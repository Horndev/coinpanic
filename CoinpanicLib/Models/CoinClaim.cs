using CoinpanicLib.NodeConnection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoinpanicLib.Models
{
    

    public class NodeLog
    {
        [Key]
        public int NodeLogId { get; set; }
        public DateTime EventTime { get; set; }
        public string EventName { get; set; }
        public string EventMessage { get; set; }
    }

    public class CoinClaim
    {
        [Key]
        public String ClaimId { get; set; }
        public String CoinShortName { get; set; }
        public String Name { get; set; }
        public String RequestIP { get; set; }

        public String Email { get; set; }

        public string DepositAddress { get; set; }
        public virtual ICollection<InputAddress> InputAddresses { get; set; }

        /* Results */
        public String UnsignedTX { get; set; }
        public String SignedTX { get; set; }

        public double TotalValue { get; set; }
        public double MyFee { get; set; }
        public double Deposited { get; set; }
        public double MinerFee { get; set; }

        public string TransactionHash { get; set; }
        public bool WasTransmitted { get; set; }
        public bool WasMined { get; set; }

        public string BlockData { get; set; }
        public string ClaimData { get; set; }
    }

    

    public class InputAddress
    {
        [Key]
        public Guid AddressId { get; set; }

        //foreign key
        public string ClaimId { get; set; }

        public String PublicAddress { get; set; }

        public string CoinShortName { get; set; }

        public double ClaimValue { get; set; }

        public bool IsClaimed { get; set; }

        public virtual CoinClaim CoinClaim { get; set; }
    }
}