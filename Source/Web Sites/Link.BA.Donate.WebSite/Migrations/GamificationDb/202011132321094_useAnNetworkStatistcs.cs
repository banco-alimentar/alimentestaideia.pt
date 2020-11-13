namespace Link.BA.Donate.WebSite.Migrations.GamificationDb
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class useAnNetworkStatistcs : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "InvitedCount", c => c.Int(nullable: false));
            AddColumn("dbo.User", "DonatedAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.User", "DonationCount", c => c.Int(nullable: false));
            AddColumn("dbo.User", "NetworkInvitedCount", c => c.Int(nullable: false));
            AddColumn("dbo.User", "NetworkDonatedAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.User", "NetworkDonationsCount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.User", "NetworkDonationsCount");
            DropColumn("dbo.User", "NetworkDonatedAmount");
            DropColumn("dbo.User", "NetworkInvitedCount");
            DropColumn("dbo.User", "DonationCount");
            DropColumn("dbo.User", "DonatedAmount");
            DropColumn("dbo.User", "InvitedCount");
        }
    }
}
