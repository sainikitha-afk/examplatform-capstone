using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPlatform.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddExamMaxAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_attempts",
                table: "exams",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_attempts",
                table: "exams");
        }
    }
}
