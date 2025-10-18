namespace SmartAuth.Domain.Interfaces;

public interface IAuditEntity
{
    DateTime CreatedAtUtc { get; set; }
    DateTime UpdatedAtUtc { get; set; }
}