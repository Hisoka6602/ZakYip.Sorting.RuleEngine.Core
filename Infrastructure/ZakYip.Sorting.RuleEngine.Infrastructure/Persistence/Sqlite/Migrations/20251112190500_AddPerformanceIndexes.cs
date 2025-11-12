using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite.Migrations
{
    /// <summary>
    /// 添加性能优化索引 - 为日志查询添加额外索引以提升性能
    /// Add performance optimization indexes - Add additional indexes for log queries to improve performance
    /// </summary>
    public partial class AddPerformanceIndexes : Migration
    {
        /// <summary>
        /// 应用迁移：创建性能优化索引
        /// Apply migration: Create performance optimization indexes
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CommunicationLog - 新增索引
            migrationBuilder.CreateIndex(
                name: "IX_communication_logs_IsSuccess",
                table: "communication_logs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_communication_logs_IsSuccess_CreatedAt",
                table: "communication_logs",
                columns: new[] { "IsSuccess", "CreatedAt" },
                descending: new[] { false, true });

            // SorterCommunicationLog - 新增索引
            migrationBuilder.CreateIndex(
                name: "IX_sorter_comm_logs_IsSuccess",
                table: "sorter_communication_logs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_sorter_comm_logs_Type_Success_Time",
                table: "sorter_communication_logs",
                columns: new[] { "CommunicationType", "IsSuccess", "CommunicationTime" },
                descending: new[] { false, false, true });

            // DwsCommunicationLog - 新增索引
            migrationBuilder.CreateIndex(
                name: "IX_dws_comm_logs_IsSuccess",
                table: "dws_communication_logs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_dws_comm_logs_Barcode_Time",
                table: "dws_communication_logs",
                columns: new[] { "Barcode", "CommunicationTime" },
                descending: new[] { false, true });

            // ApiCommunicationLog - 新增索引
            migrationBuilder.CreateIndex(
                name: "IX_api_comm_logs_IsSuccess",
                table: "api_communication_logs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_api_comm_logs_ParcelId_RequestTime",
                table: "api_communication_logs",
                columns: new[] { "ParcelId", "RequestTime" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_api_comm_logs_DurationMs_Desc",
                table: "api_communication_logs",
                column: "DurationMs",
                descending: new bool[0]);

            // MatchingLog - 新增索引
            migrationBuilder.CreateIndex(
                name: "IX_matching_logs_IsSuccess",
                table: "matching_logs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_matching_logs_MatchedRuleId",
                table: "matching_logs",
                column: "MatchedRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_matching_logs_ChuteId_Time",
                table: "matching_logs",
                columns: new[] { "ChuteId", "MatchingTime" },
                descending: new[] { false, true });

            // ApiRequestLog - 新增索引
            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_IsSuccess",
                table: "api_request_logs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_StatusCode",
                table: "api_request_logs",
                column: "ResponseStatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_DurationMs_Desc",
                table: "api_request_logs",
                column: "DurationMs",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_api_request_logs_Path_Time",
                table: "api_request_logs",
                columns: new[] { "RequestPath", "RequestTime" },
                descending: new[] { false, true });
        }

        /// <summary>
        /// 回滚迁移：删除性能优化索引
        /// Rollback migration: Drop performance optimization indexes
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // CommunicationLog - 删除索引
            migrationBuilder.DropIndex(
                name: "IX_communication_logs_IsSuccess",
                table: "communication_logs");

            migrationBuilder.DropIndex(
                name: "IX_communication_logs_IsSuccess_CreatedAt",
                table: "communication_logs");

            // SorterCommunicationLog - 删除索引
            migrationBuilder.DropIndex(
                name: "IX_sorter_comm_logs_IsSuccess",
                table: "sorter_communication_logs");

            migrationBuilder.DropIndex(
                name: "IX_sorter_comm_logs_Type_Success_Time",
                table: "sorter_communication_logs");

            // DwsCommunicationLog - 删除索引
            migrationBuilder.DropIndex(
                name: "IX_dws_comm_logs_IsSuccess",
                table: "dws_communication_logs");

            migrationBuilder.DropIndex(
                name: "IX_dws_comm_logs_Barcode_Time",
                table: "dws_communication_logs");

            // ApiCommunicationLog - 删除索引
            migrationBuilder.DropIndex(
                name: "IX_api_comm_logs_IsSuccess",
                table: "api_communication_logs");

            migrationBuilder.DropIndex(
                name: "IX_api_comm_logs_ParcelId_RequestTime",
                table: "api_communication_logs");

            migrationBuilder.DropIndex(
                name: "IX_api_comm_logs_DurationMs_Desc",
                table: "api_communication_logs");

            // MatchingLog - 删除索引
            migrationBuilder.DropIndex(
                name: "IX_matching_logs_IsSuccess",
                table: "matching_logs");

            migrationBuilder.DropIndex(
                name: "IX_matching_logs_MatchedRuleId",
                table: "matching_logs");

            migrationBuilder.DropIndex(
                name: "IX_matching_logs_ChuteId_Time",
                table: "matching_logs");

            // ApiRequestLog - 删除索引
            migrationBuilder.DropIndex(
                name: "IX_api_request_logs_IsSuccess",
                table: "api_request_logs");

            migrationBuilder.DropIndex(
                name: "IX_api_request_logs_StatusCode",
                table: "api_request_logs");

            migrationBuilder.DropIndex(
                name: "IX_api_request_logs_DurationMs_Desc",
                table: "api_request_logs");

            migrationBuilder.DropIndex(
                name: "IX_api_request_logs_Path_Time",
                table: "api_request_logs");
        }
    }
}
