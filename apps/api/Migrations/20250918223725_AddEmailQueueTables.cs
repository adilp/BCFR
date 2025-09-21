using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberOrgApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailQueueTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create EmailCampaigns first (FK target)
            migrationBuilder.CreateTable(
                name: "EmailCampaigns",
                schema: "memberorg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    TotalRecipients = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailCampaigns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaigns_Status",
                schema: "memberorg",
                table: "EmailCampaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaigns_CreatedAt",
                schema: "memberorg",
                table: "EmailCampaigns",
                column: "CreatedAt");

            // Create EmailQueue (depends on EmailCampaigns)
            migrationBuilder.CreateTable(
                name: "EmailQueue",
                schema: "memberorg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    PlainTextBody = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailQueue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailQueue_EmailCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "memberorg",
                        principalTable: "EmailCampaigns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Campaign_Recipient",
                schema: "memberorg",
                table: "EmailQueue",
                columns: new[] { "CampaignId", "RecipientEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_Processing",
                schema: "memberorg",
                table: "EmailQueue",
                columns: new[] { "Status", "ScheduledFor", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_CampaignId",
                schema: "memberorg",
                table: "EmailQueue",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueue_UpdatedAt",
                schema: "memberorg",
                table: "EmailQueue",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailQueue",
                schema: "memberorg");

            migrationBuilder.DropTable(
                name: "EmailCampaigns",
                schema: "memberorg");
        }
    }
}
