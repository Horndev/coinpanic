using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using coinpanic_airdrop.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using CoinpanicLib.Models;

namespace coinpanic_airdrop.Database
{
    public class CoinpanicContext : DbContext
    {

        public CoinpanicContext() : base("name=Coinpanic")
        {
            
        }

        public DbSet<CoinClaim> Claims { get; set; }
        public DbSet<InputAddress> Addresses { get; set; }
        public DbSet<SeedNode> SeedNodes { get; set; }
        public DbSet<NodeLog> NodeLogs { get; set; }
        public DbSet<TxSubmitted> TxSubmitted { get; set; }
        public DbSet<NodeServices> NodeServices { get; set; }
        public DbSet<IndexCoinInfo> IndexCoinInfo { get; set; }
        public DbSet<Coupon> Coupons { get; set; }

        //Lightning Network
        public DbSet<LnTransaction> LnTransactions { get; set; }
        public DbSet<LnUser> LnCommunityUsers { get; set; }
        public DbSet<LnCJUser> LnCommunityJarUsers { get; set; }
        public DbSet<LnCommunityJar> LnCommunityJars { get; set; }
        public DbSet<LnChannel> LnChannels { get; set; }
        public DbSet<LnNode> LnNodes { get; set; }
        public DbSet<LnChannelConnectionPoints> LnChannelHistory { get; set; }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}