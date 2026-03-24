using System.ComponentModel.DataAnnotations;

namespace ERP.Contracts.Infrastructure;

/// <summary>
/// Tracks processed saga messages for consumer idempotency.
/// Stored in each service's database to ensure DB operations and
/// idempotency records are committed atomically.
/// </summary>
public class ProcessedMessage
{
    [Key]
    public long Id { get; set; }

    [Required]
    public Guid CorrelationId { get; set; }

    [Required]
    [MaxLength(128)]
    public string ConsumerName { get; set; } = string.Empty;

    public bool Success { get; set; }

    /// <summary>
    /// Serialized response event data, used to re-produce the response
    /// if a duplicate message is received (handles lost Kafka produce).
    /// </summary>
    [MaxLength(4096)]
    public string? ResponseData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
