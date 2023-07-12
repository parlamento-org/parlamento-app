using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class GoogleLogInSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "CDS-PP");

            migrationBuilder.AddColumn<string>(
                name: "googleAccessToken",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "googleIDToken",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "CDSPP", "Coligação Democrática Unitária" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "CDSPP");

            migrationBuilder.DropColumn(
                name: "googleAccessToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "googleIDToken",
                table: "Users");

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "CDS-PP", "Coligação Democrática Unitária" });
        }
    }
}
