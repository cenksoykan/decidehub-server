using Microsoft.EntityFrameworkCore.Migrations;

namespace Decidehub.Infrastructure.Migrations
{
    public partial class PolicyPoll : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Polls_Policies_PolicyId", "Polls");

            migrationBuilder.DropColumn("IsPublic", "Polls");

            migrationBuilder.AddForeignKey("FK_Polls_Policies_PolicyId", "Polls", "PolicyId", "Policies",
                principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Polls_Policies_PolicyId", "Polls");

            migrationBuilder.AddColumn<bool>("IsPublic", "Polls", nullable: false, defaultValue: false);

            migrationBuilder.AddForeignKey("FK_Polls_Policies_PolicyId", "Polls", "PolicyId", "Policies",
                principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
    }
}