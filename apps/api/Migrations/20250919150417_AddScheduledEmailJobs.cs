using System;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberOrgApi.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledEmailJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledEmailJobs",
                schema: "memberorg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecurrenceRule = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NextRunDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRunDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    RunCount = table.Column<int>(type: "integer", nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<JsonObject>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledEmailJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledEmailJobs_Active",
                schema: "memberorg",
                table: "ScheduledEmailJobs",
                columns: new[] { "Status", "ScheduledFor", "NextRunDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledEmailJobs_Entity",
                schema: "memberorg",
                table: "ScheduledEmailJobs",
                columns: new[] { "EntityType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledEmailJobs",
                schema: "memberorg");
        }
    }
}
