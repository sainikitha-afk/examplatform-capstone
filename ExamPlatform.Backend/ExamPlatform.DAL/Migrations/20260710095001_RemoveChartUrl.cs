using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPlatform.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChartUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chart_url",
                table: "attempts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "chart_url",
                table: "attempts",
                type: "text",
                nullable: true);
        }
    }
}
