using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationTypeToLogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CommunicationType",
                table: "sorter_communication_logs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CommunicationType",
                table: "dws_communication_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommunicationType",
                table: "api_communication_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_sorter_comm_logs_Type",
                table: "sorter_communication_logs",
                column: "CommunicationType");

            migrationBuilder.CreateIndex(
                name: "IX_dws_comm_logs_Type",
                table: "dws_communication_logs",
                column: "CommunicationType");

            migrationBuilder.CreateIndex(
                name: "IX_api_comm_logs_Type",
                table: "api_communication_logs",
                column: "CommunicationType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sorter_comm_logs_Type",
                table: "sorter_communication_logs");

            migrationBuilder.DropIndex(
                name: "IX_dws_comm_logs_Type",
                table: "dws_communication_logs");

            migrationBuilder.DropIndex(
                name: "IX_api_comm_logs_Type",
                table: "api_communication_logs");

            migrationBuilder.DropColumn(
                name: "CommunicationType",
                table: "dws_communication_logs");

            migrationBuilder.DropColumn(
                name: "CommunicationType",
                table: "api_communication_logs");

            migrationBuilder.AlterColumn<string>(
                name: "CommunicationType",
                table: "sorter_communication_logs",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
