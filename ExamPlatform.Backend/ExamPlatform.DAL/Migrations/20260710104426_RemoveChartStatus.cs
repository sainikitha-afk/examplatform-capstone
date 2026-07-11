using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPlatform.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChartStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chart_status",
                table: "attempts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "chart_status",
                table: "attempts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
