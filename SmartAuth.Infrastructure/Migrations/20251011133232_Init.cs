using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace SmartAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: true),
                    RiskScore = table.Column<double>(type: "double precision", nullable: true),
                    Ip = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "bytea", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: true),
                    Trusted = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoveryCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TotpSecrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Secret = table.Column<string>(type: "text", nullable: false),
                    Issuer = table.Column<string>(type: "text", nullable: false),
                    Enforced = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TotpSecrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TotpSecrets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthenticators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnrolled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthenticators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuthenticators_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaceTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthenticatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(512)", nullable: false),
                    ModelVersion = table.Column<string>(type: "text", nullable: false),
                    LivenessThreshold = table.Column<float>(type: "real", nullable: false),
                    QualityScore = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceTemplates_UserAuthenticators_AuthenticatorId",
                        column: x => x.AuthenticatorId,
                        principalTable: "UserAuthenticators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoiceTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthenticatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(256)", nullable: false),
                    Phrase = table.Column<string>(type: "text", nullable: false),
                    SampleRate = table.Column<int>(type: "integer", nullable: false),
                    ModelVersion = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceTemplates_UserAuthenticators_AuthenticatorId",
                        column: x => x.AuthenticatorId,
                        principalTable: "UserAuthenticators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthAttempts_CreatedAt",
                table: "AuthAttempts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId",
                table: "Devices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplates_AuthenticatorId",
                table: "FaceTemplates",
                column: "AuthenticatorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryCodes_UserId",
                table: "RecoveryCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TotpSecrets_UserId",
                table: "TotpSecrets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthenticators_UserId",
                table: "UserAuthenticators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceTemplates_AuthenticatorId",
                table: "VoiceTemplates",
                column: "AuthenticatorId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "AuthAttempts");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "FaceTemplates");

            migrationBuilder.DropTable(
                name: "RecoveryCodes");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "TotpSecrets");

            migrationBuilder.DropTable(
                name: "VoiceTemplates");

            migrationBuilder.DropTable(
                name: "UserAuthenticators");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
