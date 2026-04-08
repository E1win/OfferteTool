using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionAndAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderSubmissions_Organisations_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenderSubmissions_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    NumericValue = table.Column<decimal>(type: "numeric", nullable: true),
                    TextValue = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderAnswers_TenderQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "TenderQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenderAnswers_TenderSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "TenderSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_TenderAnswers_QuestionId",
                table: "TenderAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAnswers_SubmissionId_QuestionId",
                table: "TenderAnswers",
                columns: new[] { "SubmissionId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderSubmissions_SupplierId",
                table: "TenderSubmissions",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderSubmissions_TenderId_SupplierId",
                table: "TenderSubmissions",
                columns: new[] { "TenderId", "SupplierId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChoiceAnswerSelections");

            migrationBuilder.DropTable(
                name: "TenderAnswers");

            migrationBuilder.DropTable(
                name: "TenderSubmissions");
        }
    }
}
