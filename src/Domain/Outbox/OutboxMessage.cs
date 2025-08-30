namespace Domain.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; set; }
    public required string Type { get; set; }
    public required string Content { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}