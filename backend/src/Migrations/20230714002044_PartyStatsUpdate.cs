using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class PartyStatsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PartyAffection",
                table: "PartyStats",
                newName: "totalAmountOfProposalsVotedOn");

            migrationBuilder.AddColumn<double>(
                name: "PartyAffectionScore",
                table: "PartyStats",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "totalAffectionPoints",
                table: "PartyStats",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartyAffectionScore",
                table: "PartyStats");

            migrationBuilder.DropColumn(
                name: "totalAffectionPoints",
                table: "PartyStats");

            migrationBuilder.RenameColumn(
                name: "totalAmountOfProposalsVotedOn",
                table: "PartyStats",
                newName: "PartyAffection");
        }
    }
}
