namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLabelAndTimeToSeedNode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SeedNode", "LastDisconnect", c => c.DateTime());
            AddColumn("dbo.SeedNode", "LastConnect", c => c.DateTime());
            AddColumn("dbo.SeedNode", "Label", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.SeedNode", "Label");
            DropColumn("dbo.SeedNode", "LastConnect");
            DropColumn("dbo.SeedNode", "LastDisconnect");
        }
    }
}
