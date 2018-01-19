namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.InputAddress",
                c => new
                    {
                        AddressId = c.Guid(nullable: false),
                        ClaimId = c.String(maxLength: 128),
                        PublicAddress = c.String(),
                        CoinShortName = c.String(),
                        ClaimValue = c.Double(nullable: false),
                        IsClaimed = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.AddressId)
                .ForeignKey("dbo.CoinClaim", t => t.ClaimId)
                .Index(t => t.ClaimId);
            
            CreateTable(
                "dbo.CoinClaim",
                c => new
                    {
                        ClaimId = c.String(nullable: false, maxLength: 128),
                        CoinShortName = c.String(),
                        Name = c.String(),
                        RequestIP = c.String(),
                        Email = c.String(),
                        DepositAddress = c.String(),
                        UnsignedTX = c.String(),
                        SignedTX = c.String(),
                        WasTransmitted = c.Boolean(nullable: false),
                        TransactionID = c.Boolean(nullable: false),
                        WasMined = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ClaimId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.InputAddress", "ClaimId", "dbo.CoinClaim");
            DropIndex("dbo.InputAddress", new[] { "ClaimId" });
            DropTable("dbo.CoinClaim");
            DropTable("dbo.InputAddress");
        }
    }
}
