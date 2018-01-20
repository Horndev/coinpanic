namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSeedNodeWithInt : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SeedNode",
                c => new
                    {
                        SeedNodeId = c.Int(nullable: false, identity: true),
                        Coin = c.String(),
                        IP = c.String(),
                        Port = c.Int(nullable: false),
                        Enabled = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.SeedNodeId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.SeedNode");
        }
    }
}
