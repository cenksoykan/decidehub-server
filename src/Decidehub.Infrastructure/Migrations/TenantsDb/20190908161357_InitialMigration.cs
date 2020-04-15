using Microsoft.EntityFrameworkCore.Migrations;

namespace Decidehub.Infrastructure.Migrations.TenantsDb
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    HostName = table.Column<string>(nullable: false),
                    Lang = table.Column<string>(nullable: true),
                    InActive = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => new { x.Id, x.HostName });
                    table.UniqueConstraint("AK_Tenants_Id", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
