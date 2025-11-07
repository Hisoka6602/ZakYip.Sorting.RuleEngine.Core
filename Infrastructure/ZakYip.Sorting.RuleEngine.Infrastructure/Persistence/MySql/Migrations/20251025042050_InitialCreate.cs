using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "api_communication_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ParcelId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestBody = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestHeaders = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    ResponseTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResponseBody = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    ResponseHeaders = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FormattedCurl = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_communication_logs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "api_request_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RequestTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RequestIp = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestMethod = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestPath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QueryString = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestHeaders = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestBody = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    ResponseHeaders = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseBody = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_request_logs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chutes",
                columns: table => new
                {
                    ChuteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChuteName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChuteCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chutes", x => x.ChuteId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "communication_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CommunicationType = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    ParcelId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemoteAddress = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_logs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dws_communication_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DwsAddress = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalContent = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FormattedContent = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Barcode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CommunicationTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dws_communication_logs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "log_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Level = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Details = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_entries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "matching_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ParcelId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DwsContent = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiContent = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MatchedRuleId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MatchingReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChuteId = table.Column<long>(type: "bigint", nullable: true),
                    CartOccupancy = table.Column<int>(type: "int", nullable: false),
                    MatchingTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matching_logs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sorter_communication_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SorterAddress = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommunicationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalContent = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FormattedContent = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExtractedParcelId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExtractedCartNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommunicationTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sorter_communication_logs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_api_comm_logs_ParcelId",
                table: "api_communication_logs",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_api_comm_logs_RequestTime_Desc",
                table: "api_communication_logs",
                column: "RequestTime",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_Method_Time",
                table: "api_request_logs",
                columns: new[] { "RequestMethod", "RequestTime" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_RequestIp",
                table: "api_request_logs",
                column: "RequestIp");

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_RequestPath",
                table: "api_request_logs",
                column: "RequestPath");

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_RequestTime_Desc",
                table: "api_request_logs",
                column: "RequestTime",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_chutes_ChuteCode",
                table: "chutes",
                column: "ChuteCode");

            migrationBuilder.CreateIndex(
                name: "IX_chutes_ChuteName",
                table: "chutes",
                column: "ChuteName");

            migrationBuilder.CreateIndex(
                name: "IX_communication_logs_CreatedAt_Desc",
                table: "communication_logs",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_communication_logs_ParcelId",
                table: "communication_logs",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_communication_logs_Type_CreatedAt",
                table: "communication_logs",
                columns: new[] { "CommunicationType", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_dws_comm_logs_Barcode",
                table: "dws_communication_logs",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_dws_comm_logs_Time_Desc",
                table: "dws_communication_logs",
                column: "CommunicationTime",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_CreatedAt_Desc",
                table: "log_entries",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_Level",
                table: "log_entries",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_Level_CreatedAt",
                table: "log_entries",
                columns: new[] { "Level", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_matching_logs_ChuteId",
                table: "matching_logs",
                column: "ChuteId");

            migrationBuilder.CreateIndex(
                name: "IX_matching_logs_ParcelId",
                table: "matching_logs",
                column: "ParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_matching_logs_Time_Desc",
                table: "matching_logs",
                column: "MatchingTime",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_sorter_comm_logs_ParcelId",
                table: "sorter_communication_logs",
                column: "ExtractedParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_sorter_comm_logs_Time_Desc",
                table: "sorter_communication_logs",
                column: "CommunicationTime",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_communication_logs");

            migrationBuilder.DropTable(
                name: "api_request_logs");

            migrationBuilder.DropTable(
                name: "chutes");

            migrationBuilder.DropTable(
                name: "communication_logs");

            migrationBuilder.DropTable(
                name: "dws_communication_logs");

            migrationBuilder.DropTable(
                name: "log_entries");

            migrationBuilder.DropTable(
                name: "matching_logs");

            migrationBuilder.DropTable(
                name: "sorter_communication_logs");
        }
    }
}
