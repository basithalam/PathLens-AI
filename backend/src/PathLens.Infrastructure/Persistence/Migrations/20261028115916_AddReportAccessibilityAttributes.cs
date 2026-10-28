using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportAccessibilityAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttributeSource",
                table: "accessibility_reports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasRamp",
                table: "accessibility_reports",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasStairs",
                table: "accessibility_reports",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Surface",
                table: "accessibility_reports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttributeSource",
                table: "accessibility_reports");

            migrationBuilder.DropColumn(
                name: "HasRamp",
                table: "accessibility_reports");

            migrationBuilder.DropColumn(
                name: "HasStairs",
                table: "accessibility_reports");

            migrationBuilder.DropColumn(
                name: "Surface",
                table: "accessibility_reports");
        }
    }
}
