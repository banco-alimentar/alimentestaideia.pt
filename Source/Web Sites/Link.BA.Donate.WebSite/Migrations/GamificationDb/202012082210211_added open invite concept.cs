namespace Link.BA.Donate.WebSite.Migrations.GamificationDb
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addedopeninviteconcept : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Invite", "IsOpen", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Invite", "IsOpen");
        }
    }
}
