using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Parlamento.Infrastructure.Persistence;

namespace Parlamento.Infrastructure.Migrations;

[DbContext(typeof(DatabaseContext))]
[Migration("20260701000400_AddStructuredDocumentContent")]
public class AddStructuredDocumentContent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "ParliamentDocumentContents"
                ADD COLUMN IF NOT EXISTS "RedactedDocumentModelJson" text NULL;

            ALTER TABLE "ParliamentDocumentContents"
                ADD COLUMN IF NOT EXISTS "ExtractorVersion" text NULL;

            ALTER TABLE "ParliamentDocumentContents"
                ADD COLUMN IF NOT EXISTS "RendererVersion" text NULL;

            ALTER TABLE "ParliamentDocumentContents"
                ADD COLUMN IF NOT EXISTS "DocumentModelSchemaVersion" text NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "ParliamentDocumentContents"
                DROP COLUMN IF EXISTS "DocumentModelSchemaVersion";

            ALTER TABLE "ParliamentDocumentContents"
                DROP COLUMN IF EXISTS "RendererVersion";

            ALTER TABLE "ParliamentDocumentContents"
                DROP COLUMN IF EXISTS "ExtractorVersion";

            ALTER TABLE "ParliamentDocumentContents"
                DROP COLUMN IF EXISTS "RedactedDocumentModelJson";
            """);
    }
}
