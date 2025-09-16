using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberOrgApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRsvpTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RsvpTokens",
                schema: "memberorg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedForResponse = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UsedWithPlusOne = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RsvpTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RsvpTokens_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "memberorg",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RsvpTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "memberorg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RsvpTokens_CreatedAt",
                schema: "memberorg",
                table: "RsvpTokens",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RsvpTokens_EventId_UserId",
                schema: "memberorg",
                table: "RsvpTokens",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RsvpTokens_ExpiresAt",
                schema: "memberorg",
                table: "RsvpTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RsvpTokens_Token",
                schema: "memberorg",
                table: "RsvpTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RsvpTokens_UserId",
                schema: "memberorg",
                table: "RsvpTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RsvpTokens",
                schema: "memberorg");
        }
    }
}
