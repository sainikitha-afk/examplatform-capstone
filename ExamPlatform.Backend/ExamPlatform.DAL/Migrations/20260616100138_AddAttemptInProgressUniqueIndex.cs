using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPlatform.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptInProgressUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_attempts_user_id",
                table: "attempts");

            migrationBuilder.CreateIndex(
                name: "uix_attempts_user_exam_inprogress",
                table: "attempts",
                columns: new[] { "user_id", "exam_id" },
                unique: true,
                filter: "attempt_status = 'InProgress'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uix_attempts_user_exam_inprogress",
                table: "attempts");

            migrationBuilder.CreateIndex(
                name: "ix_attempts_user_id",
                table: "attempts",
                column: "user_id");
        }
    }
}
