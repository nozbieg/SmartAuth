using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceBiometrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "audio_duration_seconds",
                table: "user_biometrics",
                type: "double precision",
                precision: 8,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "audio_sample_rate",
                table: "user_biometrics",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "audio_duration_seconds",
                table: "user_biometrics");

            migrationBuilder.DropColumn(
                name: "audio_sample_rate",
                table: "user_biometrics");
        }
    }
}
