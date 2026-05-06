using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderChangeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OldValue = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    NewValue = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SupplierVisibleMessage = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ChangedByDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderChangeLogs_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenderChangeLogs_TenderId_ChangedAtUtc",
                table: "TenderChangeLogs",
                columns: new[] { "TenderId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenderChangeLogs");
        }
    }
}
