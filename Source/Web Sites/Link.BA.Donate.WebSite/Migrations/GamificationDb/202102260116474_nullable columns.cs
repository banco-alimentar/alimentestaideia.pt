namespace Link.BA.Donate.WebSite.Migrations.GamificationDb
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class nullablecolumns : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Invite", "Nickname", c => c.String(maxLength: 256));
            AlterColumn("dbo.User", "Name", c => c.String(maxLength: 256));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.User", "Name", c => c.String(nullable: false, maxLength: 256));
            AlterColumn("dbo.Invite", "Nickname", c => c.String(nullable: false, maxLength: 256));
        }
    }
}
