using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberOrgApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "memberorg",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "Member");
            
            // Update existing users to have Member role
            migrationBuilder.Sql("UPDATE memberorg.\"Users\" SET \"Role\" = 'Member' WHERE \"Role\" IS NULL OR \"Role\" = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "memberorg",
                table: "Users");
        }
    }
}
