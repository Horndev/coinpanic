namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNodeServices : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.NodeServices",
                c => new
                    {
                        ServiceId = c.Int(nullable: false, identity: true),
                        Coin = c.String(),
                        Endpoint = c.String(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ServiceId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.NodeServices");
        }
    }
}
