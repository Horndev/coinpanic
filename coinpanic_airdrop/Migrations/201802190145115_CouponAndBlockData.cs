namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CouponAndBlockData : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Coupon",
                c => new
                    {
                        CouponId = c.String(nullable: false, maxLength: 128),
                        FeeRate = c.Double(nullable: false),
                        NumTimesUsed = c.Int(nullable: false),
                        MaxTimesUsed = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CouponId);
            
            AddColumn("dbo.CoinClaim", "ClaimData", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.CoinClaim", "ClaimData");
            DropTable("dbo.Coupon");
        }
    }
}
