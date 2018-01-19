namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedClaimFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CoinClaim", "MyFee", c => c.Double(nullable: false));
            AddColumn("dbo.CoinClaim", "Deposited", c => c.Double(nullable: false));
            AddColumn("dbo.CoinClaim", "MinerFee", c => c.Double(nullable: false));
            AddColumn("dbo.CoinClaim", "BlockData", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.CoinClaim", "BlockData");
            DropColumn("dbo.CoinClaim", "MinerFee");
            DropColumn("dbo.CoinClaim", "Deposited");
            DropColumn("dbo.CoinClaim", "MyFee");
        }
    }
}
