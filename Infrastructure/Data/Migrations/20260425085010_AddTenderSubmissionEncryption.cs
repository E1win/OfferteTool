using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderSubmissionEncryption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChoiceAnswerSelections");

            migrationBuilder.DropColumn(
                name: "NumericValue",
                table: "TenderAnswers");

            migrationBuilder.DropColumn(
                name: "TextValue",
                table: "TenderAnswers");

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedPayload",
                table: "TenderAnswers",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Nonce",
                table: "TenderAnswers",
                type: "bytea",
                maxLength: 12,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Tag",
                table: "TenderAnswers",
                type: "bytea",
                maxLength: 16,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedPayload",
                table: "TenderAnswers");

            migrationBuilder.DropColumn(
                name: "Nonce",
                table: "TenderAnswers");

            migrationBuilder.DropColumn(
                name: "Tag",
                table: "TenderAnswers");

            migrationBuilder.AddColumn<decimal>(
                name: "NumericValue",
                table: "TenderAnswers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextValue",
                table: "TenderAnswers",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChoiceAnswerSelections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChoiceAnswerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChoiceAnswerSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChoiceAnswerSelections_TenderAnswers_ChoiceAnswerId",
                        column: x => x.ChoiceAnswerId,
                        principalTable: "TenderAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChoiceAnswerSelections_TenderQuestionOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "TenderQuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChoiceAnswerSelections_ChoiceAnswerId_OptionId",
                table: "ChoiceAnswerSelections",
                columns: new[] { "ChoiceAnswerId", "OptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChoiceAnswerSelections_OptionId",
                table: "ChoiceAnswerSelections",
                column: "OptionId");
        }
    }
}
