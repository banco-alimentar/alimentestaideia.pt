namespace Link.BA.Donate.WebSite.Migrations.GamificationDb
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CompletedDonation",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Email = c.String(nullable: false, maxLength: 256),
                        Name = c.String(nullable: false, maxLength: 256),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 0),
                        User1Name = c.String(maxLength: 256),
                        User1Email = c.String(maxLength: 256),
                        User2Name = c.String(maxLength: 256),
                        User2Email = c.String(maxLength: 256),
                        User3Name = c.String(maxLength: 256),
                        User3Email = c.String(maxLength: 256),
                        LoadError = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Donation",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 0),
                        CreatedTs = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Invite",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FromUserId = c.Int(nullable: false),
                        ToUserId = c.Int(nullable: false),
                        Nickname = c.String(nullable: false, maxLength: 256),
                        LastPokeTs = c.DateTime(nullable: false),
                        DonationId = c.Int(nullable: false),
                        CreatedTs = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.FromUserId)
                .ForeignKey("dbo.User", t => t.ToUserId)
                .ForeignKey("dbo.Donation", t => t.DonationId)
                .Index(t => t.FromUserId)
                .Index(t => t.ToUserId)
                .Index(t => t.DonationId);
            
            CreateTable(
                "dbo.User",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 256),
                        Email = c.String(nullable: false, maxLength: 256),
                        SessionCode = c.String(nullable: false),
                        CreatedTs = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Email, unique: true);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Invite", "DonationId", "dbo.Donation");
            DropForeignKey("dbo.Invite", "ToUserId", "dbo.User");
            DropForeignKey("dbo.Invite", "FromUserId", "dbo.User");
            DropForeignKey("dbo.Donation", "UserId", "dbo.User");
            DropIndex("dbo.User", new[] { "Email" });
            DropIndex("dbo.Invite", new[] { "DonationId" });
            DropIndex("dbo.Invite", new[] { "ToUserId" });
            DropIndex("dbo.Invite", new[] { "FromUserId" });
            DropIndex("dbo.Donation", new[] { "UserId" });
            DropTable("dbo.User");
            DropTable("dbo.Invite");
            DropTable("dbo.Donation");
            DropTable("dbo.CompletedDonation");
        }
    }
}
