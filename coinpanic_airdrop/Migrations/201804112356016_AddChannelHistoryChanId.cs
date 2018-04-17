namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddChannelHistoryChanId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LnChannelConnectionPoints", "ChanId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.LnChannelConnectionPoints", "ChanId");
        }
    }
}
