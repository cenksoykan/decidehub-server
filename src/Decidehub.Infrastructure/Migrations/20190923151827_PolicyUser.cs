using Microsoft.EntityFrameworkCore.Migrations;

namespace Decidehub.Infrastructure.Migrations
{
    public partial class PolicyUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>("UserId", "Policies", nullable: true);

            migrationBuilder.CreateIndex("IX_Policies_UserId", "Policies", "UserId");

            migrationBuilder.AddForeignKey("FK_Policies_AspNetUsers_UserId", "Policies", "UserId", "AspNetUsers",
                principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Policies_AspNetUsers_UserId", "Policies");

            migrationBuilder.DropIndex("IX_Policies_UserId", "Policies");

            migrationBuilder.DropColumn("UserId", "Policies");
        }
    }
}