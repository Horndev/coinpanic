using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using CoinpanicLib.Models;

namespace NodeInterface.Database
{
    public class CoinpanicContext : DbContext
    {

        public CoinpanicContext() : base("name=Coinpanic")
        {
            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<CoinpanicContext, IoT_Service.Migrations.Configuration>("CleverMajigData"));
        }

        public DbSet<SeedNode> SeedNodes { get; set; }
        public DbSet<CoinClaim> Claims { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}