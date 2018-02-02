namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTxSubmitted : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TxSubmitted",
                c => new
                    {
                        txsId = c.Int(nullable: false, identity: true),
                        Coin = c.String(),
                        TransactionHash = c.String(),
                        IsError = c.Boolean(nullable: false),
                        IsTransmitted = c.Boolean(nullable: false),
                        ResultMessage = c.String(),
                        ClaimId = c.String(),
                        SignedTx = c.String(),
                    })
                .PrimaryKey(t => t.txsId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.TxSubmitted");
        }
    }
}
