using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoliticalParties",
                columns: table => new
                {
                    partyAcronym = table.Column<string>(type: "TEXT", nullable: false),
                    fullName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticalParties", x => x.partyAcronym);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    ProfilePic = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VotingResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    isUninamous = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingResult", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PoliticalPartypartyAcronym = table.Column<string>(type: "TEXT", nullable: false),
                    PartyAffection = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyStats_PoliticalParties_PoliticalPartypartyAcronym",
                        column: x => x.PoliticalPartypartyAcronym,
                        principalTable: "PoliticalParties",
                        principalColumn: "partyAcronym",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartyStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectLaws",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Legislatura = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    VoteDate = table.Column<string>(type: "TEXT", nullable: false),
                    ProposingPartypartyAcronym = table.Column<string>(type: "TEXT", nullable: false),
                    ProposalTitle = table.Column<string>(type: "TEXT", nullable: false),
                    FullProposalTextLink = table.Column<string>(type: "TEXT", nullable: false),
                    ProposalResult = table.Column<int>(type: "INTEGER", nullable: true),
                    VotingResultGeneralityId = table.Column<int>(type: "INTEGER", nullable: true),
                    VotingResultSpecialityId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectLaws", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectLaws_PoliticalParties_ProposingPartypartyAcronym",
                        column: x => x.ProposingPartypartyAcronym,
                        principalTable: "PoliticalParties",
                        principalColumn: "partyAcronym",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectLaws_VotingResult_VotingResultGeneralityId",
                        column: x => x.VotingResultGeneralityId,
                        principalTable: "VotingResult",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectLaws_VotingResult_VotingResultSpecialityId",
                        column: x => x.VotingResultSpecialityId,
                        principalTable: "VotingResult",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VotingBlock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    isUninamousWithinParty = table.Column<bool>(type: "INTEGER", nullable: true),
                    numberOfDeputies = table.Column<int>(type: "INTEGER", nullable: true),
                    politicalPartyAcronym = table.Column<string>(type: "TEXT", nullable: false),
                    votingOrientation = table.Column<int>(type: "INTEGER", nullable: false),
                    VotingResultId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingBlock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotingBlock_VotingResult_VotingResultId",
                        column: x => x.VotingResultId,
                        principalTable: "VotingResult",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Vote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VoteDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProjectLawId = table.Column<int>(type: "INTEGER", nullable: false),
                    VotingOrientation = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vote_ProjectLaws_ProjectLawId",
                        column: x => x.ProjectLawId,
                        principalTable: "ProjectLaws",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vote_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "BE", "Bloco de Esquerda" });

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "CDS-PP", "Coligação Democrática Unitária" });

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "CH", "Chega" });

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "Governo", "Governo" });

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "IL", "Iniciativa Liberal" });

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "L", "Partido Livre" });

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "PAN", "Pessoas-Animais-Natureza" });

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "PCP", "Partido Comunista Português" });

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "PS", "Partido Socialista" });

            migrationBuilder.InsertData(
                table: "PoliticalParties",
                columns: new[] { "partyAcronym", "fullName" },
                values: new object[] { "PSD", "Partido Social-Democrata" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyStats_PoliticalPartypartyAcronym",
                table: "PartyStats",
                column: "PoliticalPartypartyAcronym");

            migrationBuilder.CreateIndex(
                name: "IX_PartyStats_UserId",
                table: "PartyStats",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaws_ProposingPartypartyAcronym",
                table: "ProjectLaws",
                column: "ProposingPartypartyAcronym");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaws_VotingResultGeneralityId",
                table: "ProjectLaws",
                column: "VotingResultGeneralityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaws_VotingResultSpecialityId",
                table: "ProjectLaws",
                column: "VotingResultSpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_ProjectLawId",
                table: "Vote",
                column: "ProjectLawId");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_UserId",
                table: "Vote",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VotingBlock_VotingResultId",
                table: "VotingBlock",
                column: "VotingResultId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartyStats");

            migrationBuilder.DropTable(
                name: "Vote");

            migrationBuilder.DropTable(
                name: "VotingBlock");

            migrationBuilder.DropTable(
                name: "ProjectLaws");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "PoliticalParties");

            migrationBuilder.DropTable(
                name: "VotingResult");
        }
    }
}
