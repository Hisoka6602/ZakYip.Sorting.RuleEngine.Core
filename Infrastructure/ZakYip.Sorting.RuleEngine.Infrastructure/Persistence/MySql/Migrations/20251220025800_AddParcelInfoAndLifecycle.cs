using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddParcelInfoAndLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create parcel_infos table
            migrationBuilder.CreateTable(
                name: "parcel_infos",
                columns: table => new
                {
                    ParcelId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CartNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Length = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Width = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Height = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    TargetChute = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ActualChute = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    DecisionReason = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    MatchedRuleId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    PositionBias = table.Column<int>(type: "int", nullable: false),
                    ChuteNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    BagId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LifecycleStage = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parcel_infos", x => x.ParcelId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Create parcel_lifecycle_nodes table
            migrationBuilder.CreateTable(
                name: "parcel_lifecycle_nodes",
                columns: table => new
                {
                    NodeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ParcelId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    AdditionalDataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parcel_lifecycle_nodes", x => x.NodeId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Create indexes for parcel_infos
            migrationBuilder.CreateIndex(
                name: "IX_parcel_infos_ParcelId",
                table: "parcel_infos",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_parcel_infos_Status_CreatedAt",
                table: "parcel_infos",
                columns: new[] { "Status", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_parcel_infos_TargetChute_CreatedAt",
                table: "parcel_infos",
                columns: new[] { "TargetChute", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_parcel_infos_CompletedAt_Desc",
                table: "parcel_infos",
                column: "CompletedAt",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "IX_parcel_infos_BagId",
                table: "parcel_infos",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_parcel_infos_LifecycleStage_CreatedAt",
                table: "parcel_infos",
                columns: new[] { "LifecycleStage", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_parcel_infos_CartNumber",
                table: "parcel_infos",
                column: "CartNumber");

            // Create indexes for parcel_lifecycle_nodes
            migrationBuilder.CreateIndex(
                name: "IX_parcel_lifecycle_ParcelId_EventTime",
                table: "parcel_lifecycle_nodes",
                columns: new[] { "ParcelId", "EventTime" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_parcel_lifecycle_EventTime_Desc",
                table: "parcel_lifecycle_nodes",
                column: "EventTime",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "IX_parcel_lifecycle_Stage_EventTime",
                table: "parcel_lifecycle_nodes",
                columns: new[] { "Stage", "EventTime" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parcel_infos");

            migrationBuilder.DropTable(
                name: "parcel_lifecycle_nodes");
        }
    }
}
