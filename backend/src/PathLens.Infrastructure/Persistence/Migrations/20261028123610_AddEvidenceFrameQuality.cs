using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceFrameQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ImageBrightness",
                table: "evidence_items",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ImageSharpness",
                table: "evidence_items",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerceptualHash",
                table: "evidence_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityFlag",
                table: "evidence_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QualityNote",
                table: "evidence_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageBrightness",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "ImageSharpness",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "PerceptualHash",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "QualityFlag",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "QualityNote",
                table: "evidence_items");
        }
    }
}
