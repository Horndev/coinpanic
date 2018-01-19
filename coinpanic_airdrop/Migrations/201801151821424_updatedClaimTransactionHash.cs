namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updatedClaimTransactionHash : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CoinClaim", "TransactionHash", c => c.String());
            DropColumn("dbo.CoinClaim", "TransactionID");
        }
        
        public override void Down()
        {
            AddColumn("dbo.CoinClaim", "TransactionID", c => c.Boolean(nullable: false));
            DropColumn("dbo.CoinClaim", "TransactionHash");
        }
    }
}
