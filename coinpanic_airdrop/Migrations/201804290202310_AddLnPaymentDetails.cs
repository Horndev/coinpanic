namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLnPaymentDetails : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LnTransaction", "FeePaid_Satoshi", c => c.Long());
            AddColumn("dbo.LnTransaction", "NumberOfHops", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.LnTransaction", "NumberOfHops");
            DropColumn("dbo.LnTransaction", "FeePaid_Satoshi");
        }
    }
}
