using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberOrgApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDietaryRestrictionsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "DietaryRestrictions",
                schema: "memberorg",
                table: "Users",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DietaryRestrictions",
                schema: "memberorg",
                table: "Users");
        }
    }
}
