using LightningLib.lndrpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace coinpanic_airdrop.Models
{
    /// <summary>
    /// Record of channel uptime and activity
    /// </summary>
    public class LnChannelConnectionPoints
    {
        [Key]
        public int Id { get; set; }

        [Index]
        public LnNode RemoteNode { get; set; }

        [Index]
        public Int64 ChanId { get; set; }

        public DateTime? Timestamp { get; set; }

        public Int64 LocalBalance { get; set; }

        public Int64 RemoteBalance { get; set; }

        public bool IsConnected { get; set; }
    }

    /// <summary>
    /// A lightning Node
    /// </summary>
    public class LnNode
    {
        [Key]
        public string PubKey { get; set; }
        public string Alias { get; set; }
        public string Color { get; set; }
        public int last_update { get; set; }

        public virtual ICollection<LnChannel> Channels { get; set; }
    }

    public class LnChannel
    {
        [Key]
        public string ChannelId { get; set; }

        public string ChanPoint { get; set; }
        public int LastUpdate { get; set; }
        public LnNode Node1 { get; set; }
        public LnNode Node2 { get; set; }
        public Int64 Capacity { get; set; }
    }

    public class LnCJTransactions
    {
        public List<LnCJTransaction> Transactions { get; set; }
        public Int64 Balance { get; set; }
    }

    public class LnCJTransaction
    {
        public DateTime Timestamp { get; set; }
        public Int64 Amount { get; set; }
        public string Memo { get; set; }
        public string Type { get; set; }
        public int Id { get; set; }
    }

    public class LnRequestInvoice
    {
        [Required]
        public string Amount { get; set; }
    }

    public class LnRequestInvoiceResponse
    {
        public string Invoice { get; set; }

        public string Result { get; set; }
    }

    public class LnUser
    {
        /// <summary>
        /// UniqueUserID for the LN wallet
        /// </summary>
        [Key]
        public string UserId { get; set; }

        /// <summary>
        /// User current balance
        /// </summary>
        public Int64 Balance { get; set; }

        /// <summary>
        /// Collection of transactions by user
        /// </summary>
        public virtual ICollection<LnTransaction> Transactions { get; set; }
    }

    public class LnCommunityJar
    {
        [Key]
        public string JarId { get; set; }

        /// <summary>
        /// Jar Balance
        /// </summary>
        public Int64 Balance { get; set; }

        public bool IsTestnet { get; set; }

        public virtual ICollection<LnTransaction> Transactions { get; set; }

        public virtual ICollection<LnCJUser> Users { get; set; }
    }

    public class LnCJUser
    {
        [Key]
        public string LnCJUserId { get; set; }

        /// <summary>
        /// Foreign Key
        /// </summary>
        public string JarId { get; set; }

        /// <summary>
        /// IP address of user
        /// </summary>
        public string UserIP { get; set; }

        public Int64 TotalDeposited { get; set; }

        public Int64 TotalWithdrawn { get; set; }

        public int NumDeposits { get; set; }

        public int NumWithdraws { get; set; }

        public DateTime? TimesampLastWithdraw { get; set; }

        public DateTime? TimesampLastDeposit { get; set; }

    }

    public class LnTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        /// <summary>
        /// Foreign Key
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Foreign Key
        /// </summary>
        public string JarId { get; set; }

        public string HashStr { get; set; }

        /// <summary>
        /// When the transaction was executed
        /// </summary>
        public DateTime? TimestampSettled { get; set; }

        public DateTime? TimestampCreated { get; set; }

        /// <summary>
        /// Whether or not the transaction is incomming or outgoing
        /// </summary>
        public bool IsDeposit { get; set; }

        public bool IsSettled { get; set; }

        /// <summary>
        /// Whether or not the transaction is on testnet
        /// </summary>
        public bool IsTestnet { get; set; }

        public string Memo { get; set; }

        public Int64 Value { get; set; }

        public string PaymentRequest { get; set; }

        /// <summary>
        /// The fee which was paid (in Satoshi)
        /// </summary>
        public Int64? FeePaid_Satoshi { get; set; }

        /// <summary>
        /// The number of LN node hops required
        /// </summary>
        public int? NumberOfHops { get; set; }

        /// <summary>
        /// Where the payment went
        /// </summary>
        public string DestinationPubKey { get; set; }

        /// <summary>
        /// Record error messages
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Record of payment error
        /// </summary>
        public bool IsError { get; set; }
    }
}