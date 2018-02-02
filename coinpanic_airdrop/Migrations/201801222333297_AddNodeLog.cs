namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNodeLog : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.NodeLog",
                c => new
                    {
                        NodeLogId = c.Int(nullable: false, identity: true),
                        EventTime = c.DateTime(nullable: false),
                        EventName = c.String(),
                        EventMessage = c.String(),
                    })
                .PrimaryKey(t => t.NodeLogId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.NodeLog");
        }
    }
}
