namespace Link.BA.Donate.WebSite.Migrations.GamificationDb
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Aninvitedoesnotrequireadonation : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Invite", new[] { "DonationId" });
            AlterColumn("dbo.Invite", "DonationId", c => c.Int());
            CreateIndex("dbo.Invite", "DonationId");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Invite", new[] { "DonationId" });
            AlterColumn("dbo.Invite", "DonationId", c => c.Int(nullable: false));
            CreateIndex("dbo.Invite", "DonationId");
        }
    }
}
