using Microsoft.EntityFrameworkCore.Migrations;

namespace Decidehub.Infrastructure.Migrations
{
    public partial class StripeCustomer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StripeCustomers_AspNetUsers_UserId1",
                table: "StripeCustomers");

            migrationBuilder.DropIndex(
                name: "IX_StripeCustomers_UserId1",
                table: "StripeCustomers");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "StripeCustomers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "StripeCustomers",
                newName: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers",
                column: "TenantId",
                unique: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_StripeCustomers_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "StripeCustomers",
                newName: "UserId");

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "StripeCustomers",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeCustomers_UserId1",
                table: "StripeCustomers",
                column: "UserId1");
        }
    }
}
