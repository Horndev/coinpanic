namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLnNodeChannel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LnChannel",
                c => new
                    {
                        ChannelId = c.String(nullable: false, maxLength: 128),
                        ChanPoint = c.String(),
                        LastUpdate = c.Int(nullable: false),
                        Capacity = c.Long(nullable: false),
                        LnNode_PubKey = c.String(maxLength: 128),
                        Node1_PubKey = c.String(maxLength: 128),
                        Node2_PubKey = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ChannelId)
                .ForeignKey("dbo.LnNode", t => t.LnNode_PubKey)
                .ForeignKey("dbo.LnNode", t => t.Node1_PubKey)
                .ForeignKey("dbo.LnNode", t => t.Node2_PubKey)
                .Index(t => t.LnNode_PubKey)
                .Index(t => t.Node1_PubKey)
                .Index(t => t.Node2_PubKey);
            
            CreateTable(
                "dbo.LnNode",
                c => new
                    {
                        PubKey = c.String(nullable: false, maxLength: 128),
                        Alias = c.String(),
                        Color = c.String(),
                        last_update = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PubKey);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LnChannel", "Node2_PubKey", "dbo.LnNode");
            DropForeignKey("dbo.LnChannel", "Node1_PubKey", "dbo.LnNode");
            DropForeignKey("dbo.LnChannel", "LnNode_PubKey", "dbo.LnNode");
            DropIndex("dbo.LnChannel", new[] { "Node2_PubKey" });
            DropIndex("dbo.LnChannel", new[] { "Node1_PubKey" });
            DropIndex("dbo.LnChannel", new[] { "LnNode_PubKey" });
            DropTable("dbo.LnNode");
            DropTable("dbo.LnChannel");
        }
    }
}
