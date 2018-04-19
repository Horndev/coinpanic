namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCPExplorerURL : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.IndexCoinInfo", "ExplorerURL", c => c.String());
            AddColumn("dbo.IndexCoinInfo", "ExplorerUsed", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.IndexCoinInfo", "ExplorerUsed");
            DropColumn("dbo.IndexCoinInfo", "ExplorerURL");
        }
    }
}
