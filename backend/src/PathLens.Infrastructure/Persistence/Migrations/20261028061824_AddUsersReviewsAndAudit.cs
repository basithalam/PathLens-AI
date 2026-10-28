using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersReviewsAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "evidence_comparisons",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "review_audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    PreviousValue = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_audit_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_review_audit_events_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_review_audit_events_ActorUserId",
                table: "review_audit_events",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_review_audit_events_CreatedAt",
                table: "review_audit_events",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_review_audit_events_EntityType_EntityId",
                table: "review_audit_events",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "review_audit_events");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "evidence_comparisons");
        }
    }
}
