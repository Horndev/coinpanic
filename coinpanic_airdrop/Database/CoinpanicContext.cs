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
            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<CoinpanicContext, IoT_Service.Migrations.Configuration>("CleverMajigData"));
        }

        public DbSet<CoinClaim> Claims { get; set; }
        public DbSet<InputAddress> Addresses { get; set; }
        public DbSet<SeedNode> SeedNodes { get; set; }
        public DbSet<NodeLog> NodeLogs { get; set; }
        public DbSet<TxSubmitted> TxSubmitted { get; set; }
        public DbSet<NodeServices> NodeServices { get; set; }
        public DbSet<IndexCoinInfo> IndexCoinInfo { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}