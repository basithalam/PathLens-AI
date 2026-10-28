using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportDuplicates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DuplicateOfReportId",
                table: "accessibility_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PossibleDuplicateOfReportId",
                table: "accessibility_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_accessibility_reports_DuplicateOfReportId",
                table: "accessibility_reports",
                column: "DuplicateOfReportId");

            migrationBuilder.CreateIndex(
                name: "IX_accessibility_reports_PossibleDuplicateOfReportId",
                table: "accessibility_reports",
                column: "PossibleDuplicateOfReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_accessibility_reports_DuplicateOfReportId",
                table: "accessibility_reports");

            migrationBuilder.DropIndex(
                name: "IX_accessibility_reports_PossibleDuplicateOfReportId",
                table: "accessibility_reports");

            migrationBuilder.DropColumn(
                name: "DuplicateOfReportId",
                table: "accessibility_reports");

            migrationBuilder.DropColumn(
                name: "PossibleDuplicateOfReportId",
                table: "accessibility_reports");
        }
    }
}
