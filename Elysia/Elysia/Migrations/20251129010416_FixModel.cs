using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elysia.Migrations
{
    /// <inheritdoc />
    public partial class FixModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReviewDate",
                table: "Reviews",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CompletedDate",
                table: "LectureCompletions",
                newName: "CompletionDate");

            migrationBuilder.RenameColumn(
                name: "CommentText",
                table: "Discussions",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "CommentDate",
                table: "Discussions",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Reviews",
                newName: "ReviewDate");

            migrationBuilder.RenameColumn(
                name: "CompletionDate",
                table: "LectureCompletions",
                newName: "CompletedDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Discussions",
                newName: "CommentDate");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "Discussions",
                newName: "CommentText");
        }
    }
}
