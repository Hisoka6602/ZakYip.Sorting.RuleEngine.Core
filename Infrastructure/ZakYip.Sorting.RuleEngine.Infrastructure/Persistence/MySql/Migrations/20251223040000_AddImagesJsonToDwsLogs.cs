using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql.Migrations
{
    /// <summary>
    /// 添加 ImagesJson 列到 DwsCommunicationLogs 表
    /// Add ImagesJson column to DwsCommunicationLogs table
    /// </summary>
    public partial class AddImagesJsonToDwsLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagesJson",
                table: "DwsCommunicationLogs",
                type: "text",
                nullable: true,
                comment: "图片信息（JSON格式存储）/ Images information stored in JSON format");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagesJson",
                table: "DwsCommunicationLogs");
        }
    }
}
