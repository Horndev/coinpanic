namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExchangeList : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ExchangeInfo",
                c => new
                    {
                        ExchangeId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        URL = c.String(),
                        Confirmed = c.String(),
                        Rating = c.Int(nullable: false),
                        KYC = c.String(),
                    })
                .PrimaryKey(t => t.ExchangeId);
            
            CreateTable(
                "dbo.ExchangeInfoIndexCoinInfo",
                c => new
                    {
                        ExchangeInfo_ExchangeId = c.Int(nullable: false),
                        IndexCoinInfo_InfoId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ExchangeInfo_ExchangeId, t.IndexCoinInfo_InfoId })
                .ForeignKey("dbo.ExchangeInfo", t => t.ExchangeInfo_ExchangeId, cascadeDelete: true)
                .ForeignKey("dbo.IndexCoinInfo", t => t.IndexCoinInfo_InfoId, cascadeDelete: true)
                .Index(t => t.ExchangeInfo_ExchangeId)
                .Index(t => t.IndexCoinInfo_InfoId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ExchangeInfoIndexCoinInfo", "IndexCoinInfo_InfoId", "dbo.IndexCoinInfo");
            DropForeignKey("dbo.ExchangeInfoIndexCoinInfo", "ExchangeInfo_ExchangeId", "dbo.ExchangeInfo");
            DropIndex("dbo.ExchangeInfoIndexCoinInfo", new[] { "IndexCoinInfo_InfoId" });
            DropIndex("dbo.ExchangeInfoIndexCoinInfo", new[] { "ExchangeInfo_ExchangeId" });
            DropTable("dbo.ExchangeInfoIndexCoinInfo");
            DropTable("dbo.ExchangeInfo");
        }
    }
}
