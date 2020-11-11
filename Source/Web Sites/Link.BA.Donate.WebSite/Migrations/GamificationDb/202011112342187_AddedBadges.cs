namespace Link.BA.Donate.WebSite.Migrations.GamificationDb
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedBadges : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "Badges", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.User", "Badges");
        }
    }
}
