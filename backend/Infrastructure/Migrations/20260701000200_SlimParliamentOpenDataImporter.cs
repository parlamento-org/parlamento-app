using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Migrations;

[DbContext(typeof(DatabaseContext))]
[Migration("20260701000200_SlimParliamentOpenDataImporter")]
public class SlimParliamentOpenDataImporter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "ProjectLaws" DROP COLUMN IF EXISTS "LegislatureStartDate";
            ALTER TABLE "ProjectLaws" DROP COLUMN IF EXISTS "LegislatureEndDate";
            ALTER TABLE "ProjectLaws" DROP COLUMN IF EXISTS "RawJson";

            ALTER TABLE "ParliamentInitiativeAuthors" DROP COLUMN IF EXISTS "RawJson";
            ALTER TABLE "ParliamentInitiativeDocuments" DROP COLUMN IF EXISTS "RawJson";
            ALTER TABLE "ParliamentInitiativeEvents" DROP COLUMN IF EXISTS "RawJson";
            ALTER TABLE "ParliamentInitiativePublications" DROP COLUMN IF EXISTS "PagesJson";
            ALTER TABLE "ParliamentInitiativePublications" DROP COLUMN IF EXISTS "RawJson";
            ALTER TABLE "ParliamentInitiativeVotes" DROP COLUMN IF EXISTS "AbsencesJson";
            ALTER TABLE "ParliamentInitiativeVotes" DROP COLUMN IF EXISTS "PublicationJson";
            ALTER TABLE "ParliamentInitiativeVotes" DROP COLUMN IF EXISTS "RawJson";

            ALTER TABLE "ParliamentInitiativeInterventions" ADD COLUMN IF NOT EXISTS "VideoUrl" text NULL;
            ALTER TABLE "ParliamentInitiativeInterventions" ADD COLUMN IF NOT EXISTS "PublicationDiaryUrl" text NULL;
            ALTER TABLE "ParliamentInitiativeInterventions" DROP COLUMN IF EXISTS "Summary";
            ALTER TABLE "ParliamentInitiativeInterventions" DROP COLUMN IF EXISTS "VideoLinksJson";
            ALTER TABLE "ParliamentInitiativeInterventions" DROP COLUMN IF EXISTS "PublicationsJson";
            ALTER TABLE "ParliamentInitiativeInterventions" DROP COLUMN IF EXISTS "RawJson";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "ProjectLaws" ADD COLUMN IF NOT EXISTS "LegislatureStartDate" text NULL;
            ALTER TABLE "ProjectLaws" ADD COLUMN IF NOT EXISTS "LegislatureEndDate" text NULL;
            ALTER TABLE "ProjectLaws" ADD COLUMN IF NOT EXISTS "RawJson" text NULL;

            ALTER TABLE "ParliamentInitiativeAuthors" ADD COLUMN IF NOT EXISTS "RawJson" text NULL;
            ALTER TABLE "ParliamentInitiativeDocuments" ADD COLUMN IF NOT EXISTS "RawJson" text NULL;
            ALTER TABLE "ParliamentInitiativeEvents" ADD COLUMN IF NOT EXISTS "RawJson" text NULL;
            ALTER TABLE "ParliamentInitiativePublications" ADD COLUMN IF NOT EXISTS "PagesJson" text NULL;
            ALTER TABLE "ParliamentInitiativePublications" ADD COLUMN IF NOT EXISTS "RawJson" text NULL;
            ALTER TABLE "ParliamentInitiativeVotes" ADD COLUMN IF NOT EXISTS "AbsencesJson" text NULL;
            ALTER TABLE "ParliamentInitiativeVotes" ADD COLUMN IF NOT EXISTS "PublicationJson" text NULL;
            ALTER TABLE "ParliamentInitiativeVotes" ADD COLUMN IF NOT EXISTS "RawJson" text NULL;

            ALTER TABLE "ParliamentInitiativeInterventions" ADD COLUMN IF NOT EXISTS "Summary" text NULL;
            ALTER TABLE "ParliamentInitiativeInterventions" ADD COLUMN IF NOT EXISTS "VideoLinksJson" text NULL;
            ALTER TABLE "ParliamentInitiativeInterventions" ADD COLUMN IF NOT EXISTS "PublicationsJson" text NULL;
            ALTER TABLE "ParliamentInitiativeInterventions" ADD COLUMN IF NOT EXISTS "RawJson" text NULL;
            ALTER TABLE "ParliamentInitiativeInterventions" DROP COLUMN IF EXISTS "VideoUrl";
            ALTER TABLE "ParliamentInitiativeInterventions" DROP COLUMN IF EXISTS "PublicationDiaryUrl";
            """);
    }
}
