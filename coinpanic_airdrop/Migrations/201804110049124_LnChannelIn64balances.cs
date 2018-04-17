namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LnChannelIn64balances : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.LnChannelConnectionPoints", "LocalBalance", c => c.Long(nullable: false));
            AlterColumn("dbo.LnChannelConnectionPoints", "RemoteBalance", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.LnChannelConnectionPoints", "RemoteBalance", c => c.Int(nullable: false));
            AlterColumn("dbo.LnChannelConnectionPoints", "LocalBalance", c => c.Int(nullable: false));
        }
    }
}
