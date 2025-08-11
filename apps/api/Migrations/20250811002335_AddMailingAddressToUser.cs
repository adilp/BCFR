using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberOrgApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMailingAddressToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                schema: "memberorg",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "memberorg",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "memberorg",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "memberorg",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                schema: "memberorg",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                schema: "memberorg",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                schema: "memberorg",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "memberorg",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "memberorg",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "memberorg",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "memberorg",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                schema: "memberorg",
                table: "Users");
        }
    }
}
