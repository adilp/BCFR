using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberOrgApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsAndRsvps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                schema: "memberorg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Speaker = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SpeakerTitle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SpeakerBio = table.Column<string>(type: "text", nullable: true),
                    RsvpDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxAttendees = table.Column<int>(type: "integer", nullable: true),
                    AllowPlusOne = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalSchema: "memberorg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EventRsvps",
                schema: "memberorg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Response = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    HasPlusOne = table.Column<bool>(type: "boolean", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckedIn = table.Column<bool>(type: "boolean", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRsvps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventRsvps_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "memberorg",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventRsvps_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "memberorg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventRsvps_EventId_UserId",
                schema: "memberorg",
                table: "EventRsvps",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventRsvps_Response",
                schema: "memberorg",
                table: "EventRsvps",
                column: "Response");

            migrationBuilder.CreateIndex(
                name: "IX_EventRsvps_ResponseDate",
                schema: "memberorg",
                table: "EventRsvps",
                column: "ResponseDate");

            migrationBuilder.CreateIndex(
                name: "IX_EventRsvps_UserId",
                schema: "memberorg",
                table: "EventRsvps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedById",
                schema: "memberorg",
                table: "Events",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventDate",
                schema: "memberorg",
                table: "Events",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_Events_RsvpDeadline",
                schema: "memberorg",
                table: "Events",
                column: "RsvpDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status",
                schema: "memberorg",
                table: "Events",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventRsvps",
                schema: "memberorg");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "memberorg");
        }
    }
}
