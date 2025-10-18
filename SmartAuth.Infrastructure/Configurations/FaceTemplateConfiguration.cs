namespace SmartAuth.Infrastructure.Configurations;

public sealed class FaceTemplateConfiguration : IEntityTypeConfiguration<FaceTemplate>
{
    public void Configure(EntityTypeBuilder<FaceTemplate> e)
    {
        e.ToTable("face_templates");
        e.Property(x => x.Embedding)
            .HasColumnName("embedding")           
            .HasColumnType("vector(512)")         
            .IsRequired();

        e.Property(x => x.ModelVersion).HasMaxLength(50).HasColumnName("model_version");
        e.Property(x => x.LivenessThreshold).HasColumnName("liveness_threshold");
        e.Property(x => x.QualityScore).HasColumnName("quality_score");
    }
}