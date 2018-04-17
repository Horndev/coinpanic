namespace coinpanic_airdrop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddJarID2LnTx : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.LnTransaction", name: "LnCommunityJar_JarId", newName: "JarId");
            RenameIndex(table: "dbo.LnTransaction", name: "IX_LnCommunityJar_JarId", newName: "IX_JarId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.LnTransaction", name: "IX_JarId", newName: "IX_LnCommunityJar_JarId");
            RenameColumn(table: "dbo.LnTransaction", name: "JarId", newName: "LnCommunityJar_JarId");
        }
    }
}
