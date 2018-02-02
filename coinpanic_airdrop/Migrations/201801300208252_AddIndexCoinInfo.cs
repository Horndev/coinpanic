namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIndexCoinInfo : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.IndexCoinInfo",
                c => new
                    {
                        InfoId = c.Int(nullable: false, identity: true),
                        Coin = c.String(),
                        Status = c.String(),
                        Nodes = c.Int(nullable: false),
                        CoinName = c.String(),
                        CoinHeaderMessage = c.String(),
                        CoinNotice = c.String(),
                        AlertClass = c.String(),
                        Exchange = c.String(),
                        ExchangeURL = c.String(),
                        ExchangeConfirm = c.String(),
                    })
                .PrimaryKey(t => t.InfoId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.IndexCoinInfo");
        }
    }
}
