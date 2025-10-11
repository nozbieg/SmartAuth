namespace SmartAuth.Infrastructure.Configurations;

public sealed class VoiceTemplateConfiguration : IEntityTypeConfiguration<VoiceTemplate>
{
    public void Configure(EntityTypeBuilder<VoiceTemplate> e)
    {
        e.ToTable("voice_templates");
        e.HasKey(x => x.Id);
        e.Property(x => x.Embedding)
            .HasColumnName("embedding")
            .HasColumnType("vector(256)")
            .IsRequired();

        e.Property(x => x.Phrase).HasMaxLength(120);
        e.Property(x => x.SampleRate);
        e.Property(x => x.ModelVersion).HasMaxLength(50).HasColumnName("model_version");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}