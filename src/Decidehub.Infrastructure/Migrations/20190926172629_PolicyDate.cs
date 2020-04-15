using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Decidehub.Infrastructure.Migrations
{
    public partial class PolicyDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>("CreatedAt", "Policies", nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("CreatedAt", "Policies");
        }
    }
}