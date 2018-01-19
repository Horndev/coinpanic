namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTotalValue : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CoinClaim", "TotalValue", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CoinClaim", "TotalValue");
        }
    }
}
