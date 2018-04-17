namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLnTables : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LnCommunityJar",
                c => new
                    {
                        JarId = c.String(nullable: false, maxLength: 128),
                        Balance = c.Long(nullable: false),
                        IsTestnet = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.JarId);
            
            CreateTable(
                "dbo.LnTransaction",
                c => new
                    {
                        TransactionId = c.Int(nullable: false, identity: true),
                        UserId = c.String(maxLength: 128),
                        HashStr = c.String(),
                        TimestampSettled = c.DateTime(),
                        TimestampCreated = c.DateTime(),
                        IsDeposit = c.Boolean(nullable: false),
                        IsSettled = c.Boolean(nullable: false),
                        IsTestnet = c.Boolean(nullable: false),
                        Memo = c.String(),
                        Value = c.Long(nullable: false),
                        PaymentRequest = c.String(),
                        LnCommunityJar_JarId = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.TransactionId)
                .ForeignKey("dbo.LnCommunityJar", t => t.LnCommunityJar_JarId)
                .ForeignKey("dbo.LnUser", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.LnCommunityJar_JarId);
            
            CreateTable(
                "dbo.LnCJUser",
                c => new
                    {
                        LnCJUserId = c.Int(nullable: false, identity: true),
                        JarId = c.String(maxLength: 128),
                        UserIP = c.String(),
                        TotalDeposited = c.Long(nullable: false),
                        TotalWithdrawn = c.Long(nullable: false),
                        NumDeposits = c.Int(nullable: false),
                        NumWithdraws = c.Int(nullable: false),
                        TimesampLastWithdraw = c.DateTime(),
                        TimesampLastDeposit = c.DateTime(),
                    })
                .PrimaryKey(t => t.LnCJUserId)
                .ForeignKey("dbo.LnCommunityJar", t => t.JarId)
                .Index(t => t.JarId);
            
            CreateTable(
                "dbo.LnUser",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        Balance = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LnTransaction", "UserId", "dbo.LnUser");
            DropForeignKey("dbo.LnCJUser", "JarId", "dbo.LnCommunityJar");
            DropForeignKey("dbo.LnTransaction", "LnCommunityJar_JarId", "dbo.LnCommunityJar");
            DropIndex("dbo.LnCJUser", new[] { "JarId" });
            DropIndex("dbo.LnTransaction", new[] { "LnCommunityJar_JarId" });
            DropIndex("dbo.LnTransaction", new[] { "UserId" });
            DropTable("dbo.LnUser");
            DropTable("dbo.LnCJUser");
            DropTable("dbo.LnTransaction");
            DropTable("dbo.LnCommunityJar");
        }
    }
}
