using Application.Common.Interfaces;
using Domain.Entities; // Cần để deserialize
using Domain.Entities.Users;
using Domain.Outbox;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.BackgroundJobs;

public class ProcessOutboxMessagesJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public ProcessOutboxMessagesJob(IServiceProvider serviceProvider, ILogger<ProcessOutboxMessagesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Chờ 10 giây trước khi quét lại
        }
    }

    private async Task ProcessMessages(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var elasticService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .Take(20) 
            .ToListAsync(stoppingToken);

        foreach (var message in messages)
        {
            try
            {
              
                var userContent = JsonSerializer.Deserialize<User>(message.Content);
                if (userContent is null) continue;

                if (message.Type == "UserCreated" || message.Type == "UserUpdated")
                {
                    await elasticService.IndexUserAsync(userContent, stoppingToken);
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
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}