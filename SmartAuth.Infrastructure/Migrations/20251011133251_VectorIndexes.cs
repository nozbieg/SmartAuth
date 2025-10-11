using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartAuth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VectorIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 DO $$
                                 BEGIN
                                   IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'vector') THEN
                                     CREATE INDEX IF NOT EXISTS idx_face_embedding_ivf 
                                       ON "FaceTemplates" USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
                                     CREATE INDEX IF NOT EXISTS idx_voice_embedding_ivf 
                                       ON "VoiceTemplates" USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
                                   END IF;
                                 END$$;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
