namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLnChannelHistory : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LnChannelConnectionPoints",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Timestamp = c.DateTime(),
                        LocalBalance = c.Int(nullable: false),
                        RemoteBalance = c.Int(nullable: false),
                        IsConnected = c.Boolean(nullable: false),
                        RemoteNode_PubKey = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.LnNode", t => t.RemoteNode_PubKey)
                .Index(t => t.RemoteNode_PubKey);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LnChannelConnectionPoints", "RemoteNode_PubKey", "dbo.LnNode");
            DropIndex("dbo.LnChannelConnectionPoints", new[] { "RemoteNode_PubKey" });
            DropTable("dbo.LnChannelConnectionPoints");
        }
    }
}
