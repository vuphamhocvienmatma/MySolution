

namespace Infrastructure.BackgroundJobs;

public class ProcessOutboxMessagesJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IElasticsearchService _elasticService;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public ProcessOutboxMessagesJob(
        ApplicationDbContext dbContext,
        IElasticsearchService elasticService,
        ILogger<ProcessOutboxMessagesJob> logger)
    {
        _dbContext = dbContext;
        _elasticService = elasticService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var messages = await _dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .Take(20)
            .ToListAsync();
        foreach (var message in messages)
        {
            try
            {
                var userContent = JsonSerializer.Deserialize<User>(message.Content);
                if (userContent is not null && (message.Type == "UserCreated" || message.Type == "UserUpdated"))
                {
                    await _elasticService.IndexUserAsync(userContent);
                }
                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                message.Error = ex.Message;
            }
        }

        if (messages.Any())
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}