using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoringAlertsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monitoring_alerts",
                columns: table => new
                {
                    AlertId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CurrentValue = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: true),
                    ThresholdValue = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: true),
                    AlertTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResolvedTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AdditionalData = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitoring_alerts", x => x.AlertId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chutes_CreatedAt_Desc",
                table: "chutes",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_monitoring_alerts_AlertTime_Desc",
                table: "monitoring_alerts",
                column: "AlertTime",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_monitoring_alerts_IsResolved",
                table: "monitoring_alerts",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_monitoring_alerts_IsResolved_AlertTime",
                table: "monitoring_alerts",
                columns: new[] { "IsResolved", "AlertTime" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_monitoring_alerts_Type_AlertTime",
                table: "monitoring_alerts",
                columns: new[] { "Type", "AlertTime" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monitoring_alerts");

            migrationBuilder.DropIndex(
                name: "IX_chutes_CreatedAt_Desc",
                table: "chutes");
        }
    }
}
