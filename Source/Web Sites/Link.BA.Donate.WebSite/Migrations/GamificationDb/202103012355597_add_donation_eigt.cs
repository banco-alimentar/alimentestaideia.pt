namespace Link.BA.Donate.WebSite.Migrations.GamificationDb
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class add_donation_eigt : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CompletedDonation", "Weight", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Donation", "Weight", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.User", "DonatedWeight", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.User", "NetworkDonatedWeight", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.User", "NetworkDonatedWeight");
            DropColumn("dbo.User", "DonatedWeight");
            DropColumn("dbo.Donation", "Weight");
            DropColumn("dbo.CompletedDonation", "Weight");
        }
    }
}
