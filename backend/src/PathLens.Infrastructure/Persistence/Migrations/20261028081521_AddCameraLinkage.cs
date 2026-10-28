using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCameraLinkage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CameraSourceId",
                table: "evidence_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CameraSourceId",
                table: "accessibility_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_evidence_items_CameraSourceId",
                table: "evidence_items",
                column: "CameraSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_accessibility_reports_CameraSourceId",
                table: "accessibility_reports",
                column: "CameraSourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_evidence_items_CameraSourceId",
                table: "evidence_items");

            migrationBuilder.DropIndex(
                name: "IX_accessibility_reports_CameraSourceId",
                table: "accessibility_reports");

            migrationBuilder.DropColumn(
                name: "CameraSourceId",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "CameraSourceId",
                table: "accessibility_reports");
        }
    }
}
