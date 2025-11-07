using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_communication_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParcelId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RequestUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RequestBody = table.Column<string>(type: "TEXT", nullable: true),
                    RequestHeaders = table.Column<string>(type: "TEXT", nullable: true),
                    RequestTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    ResponseTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResponseBody = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    ResponseHeaders = table.Column<string>(type: "TEXT", nullable: true),
                    FormattedCurl = table.Column<string>(type: "TEXT", nullable: true),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_communication_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "api_request_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequestIp = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RequestMethod = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    RequestPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    QueryString = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RequestHeaders = table.Column<string>(type: "TEXT", nullable: true),
                    RequestBody = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    ResponseHeaders = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseBody = table.Column<string>(type: "TEXT", nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_request_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chutes",
                columns: table => new
                {
                    ChuteId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChuteName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ChuteCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chutes", x => x.ChuteId);
                });

            migrationBuilder.CreateTable(
                name: "communication_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommunicationType = table.Column<int>(type: "INTEGER", nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false),
                    ParcelId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    RemoteAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dws_communication_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DwsAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OriginalContent = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    FormattedContent = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Weight = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: true),
                    Volume = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: true),
                    CommunicationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dws_communication_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "log_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Level = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "matching_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParcelId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DwsContent = table.Column<string>(type: "TEXT", nullable: true),
                    ApiContent = table.Column<string>(type: "TEXT", nullable: true),
                    MatchedRuleId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MatchingReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ChuteId = table.Column<long>(type: "INTEGER", nullable: true),
                    CartOccupancy = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchingTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matching_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sorter_communication_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SorterAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CommunicationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OriginalContent = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    FormattedContent = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ExtractedParcelId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ExtractedCartNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CommunicationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sorter_communication_logs", x => x.Id);
                });

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
