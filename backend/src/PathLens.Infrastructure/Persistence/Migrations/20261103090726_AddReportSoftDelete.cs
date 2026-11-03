using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // H5 shipped IsDeleted/DeletedAt in the domain model without a migration, so the deployed
            // database was already patched by hand. Every statement is therefore guarded: a fresh clone
            // creates the schema, an already-patched database no-ops instead of aborting the update.
            //
            // The generated AlterDatabase()/postgis extension annotation is deliberately dropped:
            // PostGisHelper.TryEnableAsync() already creates the extension best-effort at startup and
            // swallows the failure, which is what keeps the local no-PostGIS cluster usable. Emitting it
            // here would make this migration fatal on any host without PostGIS binaries.
            migrationBuilder.Sql(
                "ALTER TABLE \"accessibility_reports\" ADD COLUMN IF NOT EXISTS \"IsDeleted\" boolean NOT NULL DEFAULT FALSE;");

            migrationBuilder.Sql(
                "ALTER TABLE \"accessibility_reports\" ADD COLUMN IF NOT EXISTS \"DeletedAt\" timestamp with time zone NULL;");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_accessibility_reports_IsDeleted\" ON \"accessibility_reports\" (\"IsDeleted\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_accessibility_reports_IsDeleted\";");
            migrationBuilder.Sql("ALTER TABLE \"accessibility_reports\" DROP COLUMN IF EXISTS \"DeletedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"accessibility_reports\" DROP COLUMN IF EXISTS \"IsDeleted\";");
        }
    }
}
