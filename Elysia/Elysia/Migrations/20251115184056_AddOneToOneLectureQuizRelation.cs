using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elysia.Migrations
{
    /// <inheritdoc />
    public partial class AddOneToOneLectureQuizRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quizzes_LectureID",
                table: "Quizzes");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_LectureID",
                table: "Quizzes",
                column: "LectureID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quizzes_LectureID",
                table: "Quizzes");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_LectureID",
                table: "Quizzes",
                column: "LectureID");
        }
    }
}
