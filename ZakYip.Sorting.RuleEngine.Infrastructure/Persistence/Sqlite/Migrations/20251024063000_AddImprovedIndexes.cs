using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddImprovedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 删除旧的索引
            // Drop old indexes
            migrationBuilder.DropIndex(
                name: "IX_log_entries_CreatedAt",
                table: "log_entries");

            migrationBuilder.DropIndex(
                name: "IX_log_entries_Level",
                table: "log_entries");

            // 创建新的索引
            // Create new indexes
            migrationBuilder.CreateIndex(
                name: "IX_log_entries_Level",
                table: "log_entries",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_CreatedAt_Desc",
                table: "log_entries",
                column: "CreatedAt",
                descending: new bool[] { true });

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_Level_CreatedAt",
                table: "log_entries",
                columns: new[] { "Level", "CreatedAt" },
                descending: new bool[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 删除新的索引
            // Drop new indexes
            migrationBuilder.DropIndex(
                name: "IX_log_entries_CreatedAt_Desc",
                table: "log_entries");

            migrationBuilder.DropIndex(
                name: "IX_log_entries_Level_CreatedAt",
                table: "log_entries");

            migrationBuilder.DropIndex(
                name: "IX_log_entries_Level",
                table: "log_entries");

            // 恢复旧的索引
            // Restore old indexes
            migrationBuilder.CreateIndex(
                name: "IX_log_entries_CreatedAt",
                table: "log_entries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_Level",
                table: "log_entries",
                column: "Level");
        }
    }
}
