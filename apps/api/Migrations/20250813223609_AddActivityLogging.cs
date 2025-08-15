using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberOrgApi.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                schema: "memberorg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActivityCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OldValue = table.Column<string>(type: "jsonb", nullable: true),
                    NewValue = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PerformedById = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_PerformedById",
                        column: x => x.PerformedById,
                        principalSchema: "memberorg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "memberorg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_ActivityCategory",
                schema: "memberorg",
                table: "ActivityLogs",
                column: "ActivityCategory");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_ActivityType",
                schema: "memberorg",
                table: "ActivityLogs",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CreatedAt",
                schema: "memberorg",
                table: "ActivityLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_PerformedById",
                schema: "memberorg",
                table: "ActivityLogs",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId",
                schema: "memberorg",
                table: "ActivityLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs",
                schema: "memberorg");
        }
    }
}
