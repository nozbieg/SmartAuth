using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBiometricVectorIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Indeks wektorowy ivfflat dla szybkiego wyszukiwania podobieństwa embeddingów twarzy.
            // Uwaga: IVFFlat wymaga rozszerzenia 'vector' (dodane w modelu) i wcześniejszego zaludnienia danych dla optymalnych parametrów.
            // Opcjonalnie można dostosować lists= wartości według wolumenu danych.
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_user_biometrics_embedding_ivfflat ON user_biometrics USING ivfflat (embedding) WITH (lists = 100);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_user_biometrics_embedding_ivfflat;");
        }
    }
}
