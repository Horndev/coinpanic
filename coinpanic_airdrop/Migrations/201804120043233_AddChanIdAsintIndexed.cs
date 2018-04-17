namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddChanIdAsintIndexed : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.LnChannelConnectionPoints", "ChanId", c => c.Long(nullable: false));
            CreateIndex("dbo.LnChannelConnectionPoints", "ChanId");
        }
        
        public override void Down()
        {
            DropIndex("dbo.LnChannelConnectionPoints", new[] { "ChanId" });
            AlterColumn("dbo.LnChannelConnectionPoints", "ChanId", c => c.String());
        }
    }
}
