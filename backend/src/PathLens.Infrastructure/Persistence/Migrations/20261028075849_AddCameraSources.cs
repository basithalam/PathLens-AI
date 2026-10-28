using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCameraSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "camera_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    FeedImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DirectionLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CadenceSeconds = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    PermissionNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSuccessAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastChangeAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    LastImageHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_camera_sources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_camera_sources_Enabled",
                table: "camera_sources",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_camera_sources_Provider_ExternalId",
                table: "camera_sources",
                columns: new[] { "Provider", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "camera_sources");
        }
    }
}
