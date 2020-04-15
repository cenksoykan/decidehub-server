using Microsoft.EntityFrameworkCore.Migrations;

namespace Decidehub.Infrastructure.Migrations
{
    public partial class VoterIdRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Votes_AspNetUsers_VoterId", "Votes");

            migrationBuilder.AlterColumn<string>("VoterId", "Votes", nullable: false, oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddForeignKey("FK_Votes_AspNetUsers_VoterId", "Votes", "VoterId", "AspNetUsers",
                principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Votes_AspNetUsers_VoterId", "Votes");

            migrationBuilder.AlterColumn<string>("VoterId", "Votes", nullable: true, oldClrType: typeof(string));

            migrationBuilder.AddForeignKey("FK_Votes_AspNetUsers_VoterId", "Votes", "VoterId", "AspNetUsers",
                principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
    }
}