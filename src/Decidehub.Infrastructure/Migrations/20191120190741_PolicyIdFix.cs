using Microsoft.EntityFrameworkCore.Migrations;

namespace Decidehub.Infrastructure.Migrations
{
    public partial class PolicyIdFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Polls_Policies_PolicyId", "Polls");

            migrationBuilder.AddForeignKey("FK_Polls_Policies_PolicyId", "Polls", "PolicyId", "Policies",
                principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Polls_Policies_PolicyId", "Polls");

            migrationBuilder.AddForeignKey("FK_Polls_Policies_PolicyId", "Polls", "PolicyId", "Policies",
                principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }
    }
}