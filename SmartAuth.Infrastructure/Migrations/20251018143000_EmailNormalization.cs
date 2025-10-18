using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartAuth.Infrastructure.Migrations;

public partial class EmailNormalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Znormalizuj istniejące dane (lowercase + trim)
        migrationBuilder.Sql("UPDATE users SET email = lower(trim(email));");

        // Dodaj constraint wymuszający lowercase
        migrationBuilder.Sql("ALTER TABLE users ADD CONSTRAINT ck_users_email_lowercase CHECK (email = lower(email));");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE users DROP CONSTRAINT IF EXISTS ck_users_email_lowercase;");
    }
}

