using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamPlatform.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicScoresUrlColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Repairs a gap in the migration history: the model/snapshot already
            // declare Attempt.TopicScoresUrl, but no prior migration's Up() ever
            // created the underlying column (the RemoveChartUrl migration dropped
            // chart_url without adding its topic_scores_url replacement). Without
            // this column, EF's SELECT over attempts fails with 42703.
            //
            // Idempotent by design: "ADD COLUMN IF NOT EXISTS" adds the column on
            // databases that lack it and is a safe no-op on databases that already
            // have it (e.g. the collaborator's), so no "column already exists" error.
            migrationBuilder.Sql(
                "ALTER TABLE attempts ADD COLUMN IF NOT EXISTS topic_scores_url text;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverses only this column, and only if present. Additive/non-destructive.
            migrationBuilder.Sql(
                "ALTER TABLE attempts DROP COLUMN IF EXISTS topic_scores_url;");
        }
    }
}
