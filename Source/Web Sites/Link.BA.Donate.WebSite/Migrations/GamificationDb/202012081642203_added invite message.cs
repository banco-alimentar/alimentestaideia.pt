namespace Link.BA.Donate.WebSite.Migrations.GamificationDb
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addedinvitemessage : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Invite", "Message", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Invite", "Message");
        }
    }
}
