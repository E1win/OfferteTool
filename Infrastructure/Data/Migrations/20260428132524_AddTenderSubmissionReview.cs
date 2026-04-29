using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderSubmissionReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderSubmissionReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderSubmissionReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderSubmissionReviews_AspNetUsers_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenderSubmissionReviews_TenderSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "TenderSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderQuestionReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderQuestionReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderQuestionReviews_TenderQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "TenderQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenderQuestionReviews_TenderSubmissionReviews_SubmissionRev~",
                        column: x => x.SubmissionReviewId,
                        principalTable: "TenderSubmissionReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenderQuestionReviews_QuestionId",
                table: "TenderQuestionReviews",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderQuestionReviews_SubmissionReviewId_QuestionId",
                table: "TenderQuestionReviews",
                columns: new[] { "SubmissionReviewId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderSubmissionReviews_ReviewerUserId",
                table: "TenderSubmissionReviews",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderSubmissionReviews_SubmissionId_ReviewerUserId",
                table: "TenderSubmissionReviews",
                columns: new[] { "SubmissionId", "ReviewerUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenderQuestionReviews");

            migrationBuilder.DropTable(
                name: "TenderSubmissionReviews");
        }
    }
}
