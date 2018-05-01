namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LNPaymentAddDestinationAndErrors : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LnTransaction", "DestinationPubKey", c => c.String());
            AddColumn("dbo.LnTransaction", "ErrorMessage", c => c.String());
            AddColumn("dbo.LnTransaction", "IsError", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.LnTransaction", "IsError");
            DropColumn("dbo.LnTransaction", "ErrorMessage");
            DropColumn("dbo.LnTransaction", "DestinationPubKey");
        }
    }
}
