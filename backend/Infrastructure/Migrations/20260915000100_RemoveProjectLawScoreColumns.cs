using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Migrations;

[DbContext(typeof(DatabaseContext))]
[Migration("20260915000100_RemoveProjectLawScoreColumns")]
public class RemoveProjectLawScoreColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "ProjectLaws" DROP COLUMN IF EXISTS "Score";
            ALTER TABLE "ProjectLaws" DROP COLUMN IF EXISTS "amountOfUsersInterested";
            ALTER TABLE "ProjectLaws" DROP COLUMN IF EXISTS "totalAmountOfVotesFromUsers";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "ProjectLaws" ADD COLUMN IF NOT EXISTS "Score" integer NOT NULL DEFAULT 100;
            ALTER TABLE "ProjectLaws" ADD COLUMN IF NOT EXISTS "amountOfUsersInterested" integer NOT NULL DEFAULT 0;
            ALTER TABLE "ProjectLaws" ADD COLUMN IF NOT EXISTS "totalAmountOfVotesFromUsers" integer NOT NULL DEFAULT 0;
            """);
    }
}
