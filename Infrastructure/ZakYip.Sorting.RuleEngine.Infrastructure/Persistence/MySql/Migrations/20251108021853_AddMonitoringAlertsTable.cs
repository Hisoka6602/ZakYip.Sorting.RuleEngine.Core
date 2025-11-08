using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql.Migrations
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
                    AlertId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResourceId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ThresholdValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AlertTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsResolved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ResolvedTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AdditionalData = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitoring_alerts", x => x.AlertId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
