using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceComparisons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evidence_comparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    EarlierEvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaterEvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SupportingEvidenceJson = table.Column<string>(type: "text", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedOutcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ReviewReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_comparisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evidence_comparisons_accessibility_reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "accessibility_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evidence_comparisons_evidence_items_EarlierEvidenceId",
                        column: x => x.EarlierEvidenceId,
                        principalTable: "evidence_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_comparisons_evidence_items_LaterEvidenceId",
                        column: x => x.LaterEvidenceId,
                        principalTable: "evidence_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_comparisons_CreatedAt",
                table: "evidence_comparisons",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_comparisons_EarlierEvidenceId",
                table: "evidence_comparisons",
                column: "EarlierEvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_comparisons_LaterEvidenceId",
                table: "evidence_comparisons",
                column: "LaterEvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_comparisons_ReportId",
                table: "evidence_comparisons",
                column: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence_comparisons");
        }
    }
}
