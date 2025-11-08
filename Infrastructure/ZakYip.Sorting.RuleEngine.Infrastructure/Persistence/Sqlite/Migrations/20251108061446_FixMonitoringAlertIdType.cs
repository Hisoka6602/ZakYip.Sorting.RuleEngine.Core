using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite.Migrations
{
    /// <summary>
    /// 修复监控告警表的AlertId字段类型
    /// 将AlertId从TEXT改为INTEGER自增主键
    /// </summary>
    public partial class FixMonitoringAlertIdType : Migration
    {
        /// <summary>
        /// 应用迁移：将AlertId字段类型从TEXT改为INTEGER自增
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "AlertId",
                table: "monitoring_alerts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100)
                .Annotation("Sqlite:Autoincrement", true);
        }

        /// <summary>
        /// 回滚迁移：将AlertId字段类型从INTEGER自增改回TEXT
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AlertId",
                table: "monitoring_alerts",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);
        }
    }
}
