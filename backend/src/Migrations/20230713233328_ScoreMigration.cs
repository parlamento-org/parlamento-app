using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class ScoreMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "amountOfUsersInterested",
                table: "ProjectLaws",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "totalAmountOfVotesFromUsers",
                table: "ProjectLaws",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "amountOfUsersInterested",
                table: "ProjectLaws");

            migrationBuilder.DropColumn(
                name: "totalAmountOfVotesFromUsers",
                table: "ProjectLaws");
        }
    }
}
