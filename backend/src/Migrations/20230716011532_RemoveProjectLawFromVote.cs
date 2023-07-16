using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectLawFromVote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vote_ProjectLaws_ProjectLawId",
                table: "Vote");

            migrationBuilder.DropIndex(
                name: "IX_Vote_ProjectLawId",
                table: "Vote");

            migrationBuilder.RenameColumn(
                name: "ProjectLawId",
                table: "Vote",
                newName: "ProjectLawID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProjectLawID",
                table: "Vote",
                newName: "ProjectLawId");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_ProjectLawId",
                table: "Vote",
                column: "ProjectLawId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vote_ProjectLaws_ProjectLawId",
                table: "Vote",
                column: "ProjectLawId",
                principalTable: "ProjectLaws",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
