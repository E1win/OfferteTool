using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecurityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ActorIdentifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TargetUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TargetOrganisationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DetailsJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_ActorUserId_OccurredAtUtc",
                table: "SecurityAuditLogs",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_EventType_OccurredAtUtc",
                table: "SecurityAuditLogs",
                columns: new[] { "EventType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_TargetOrganisationId_OccurredAtUtc",
                table: "SecurityAuditLogs",
                columns: new[] { "TargetOrganisationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_TargetUserId_OccurredAtUtc",
                table: "SecurityAuditLogs",
                columns: new[] { "TargetUserId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityAuditLogs");
        }
    }
}
