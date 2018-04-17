namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CJUserID2String : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.LnCJUser");
            DropColumn("dbo.LnCJUser", "LnCJUserId");
            AddColumn("dbo.LnCJUser", "LnCJUserId", c => c.String(nullable: false, maxLength: 128));
            AddPrimaryKey("dbo.LnCJUser", "LnCJUserId");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.LnCJUser");
            DropColumn("dbo.LnCJUser", "LnCJUserId");
            AddColumn("dbo.LnCJUser", "LnCJUserId", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.LnCJUser", "LnCJUserId");
        }
    }
}
