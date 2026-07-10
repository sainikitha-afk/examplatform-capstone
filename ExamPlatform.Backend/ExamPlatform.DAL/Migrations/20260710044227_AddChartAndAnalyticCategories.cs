using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPlatform.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddChartAndAnalyticCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "analytic_category",
                table: "exam_questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "chart_status",
                table: "attempts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "chart_url",
                table: "attempts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "analytic_category",
                table: "exam_questions");

            migrationBuilder.DropColumn(
                name: "chart_status",
                table: "attempts");

            migrationBuilder.DropColumn(
                name: "chart_url",
                table: "attempts");
        }
    }
}
