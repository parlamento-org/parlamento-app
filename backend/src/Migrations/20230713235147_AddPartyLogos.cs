using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPartyLogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "fullName",
                table: "PoliticalParties",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logoLink",
                table: "PoliticalParties",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "BE",
                column: "logoLink",
                value: "https://pt.wikipedia.org/wiki/Bloco_de_Esquerda");

            migrationBuilder.UpdateData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "CDSPP",
                column: "logoLink",
                value: "https://pt.m.wikipedia.org/wiki/Ficheiro:CDS_%E2%80%93_People%27s_Party_logo.svg");

            migrationBuilder.UpdateData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "CH",
                column: "logoLink",
                value: "https://en.wikipedia.org/wiki/File:Logo_of_the_Chega_(political_party).svg");

            migrationBuilder.UpdateData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "Governo",
                column: "logoLink",
                value: "https://pt.wikipedia.org/wiki/Ficheiro:Trollface.png");

            migrationBuilder.UpdateData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "IL",
                column: "logoLink",
                value: "https://pt.m.wikipedia.org/wiki/Ficheiro:Iniciativa_Liberal_logo_1.png");

            migrationBuilder.UpdateData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "L",
                column: "logoLink",
                value: "https://pt.m.wikipedia.org/wiki/Ficheiro:Partido_LIVRE_logo.png");

            migrationBuilder.UpdateData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "PAN",
                column: "logoLink",
                value: "https://pt.wikipedia.org/wiki/Pessoas%E2%80%93Animais%E2%80%93Natureza");

            migrationBuilder.UpdateData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "PCP",
                column: "logoLink",
                value: "https://pt.m.wikipedia.org/wiki/Ficheiro:Portuguese_Communist_Party_logo.svg");

            migrationBuilder.UpdateData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "PS",
                column: "logoLink",
                value: "https://pt.wikipedia.org/wiki/Ficheiro:Partido_Socialista_%28Portugal%29.png");

            migrationBuilder.UpdateData(
                table: "PoliticalParties",
                keyColumn: "partyAcronym",
                keyValue: "PSD",
                column: "logoLink",
                value: "https://pt.wikipedia.org/wiki/Ficheiro:Logo_PSD_cor.PNG");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "logoLink",
                table: "PoliticalParties");

            migrationBuilder.AlterColumn<string>(
                name: "fullName",
                table: "PoliticalParties",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
