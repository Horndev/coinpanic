namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_Claim_Timestamps : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CoinClaim", "InitializeDate", c => c.DateTime());
            AddColumn("dbo.CoinClaim", "SubmitDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.CoinClaim", "SubmitDate");
            DropColumn("dbo.CoinClaim", "InitializeDate");
        }
    }
}
