using ClaimRequest.BLL.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ClaimReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClaimReminderService> _logger;

    public ClaimReminderService(IServiceScopeFactory scopeFactory, ILogger<ClaimReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Get current time
                var now = DateTime.Now;
                
                // Calculate time until next 1:00 PM
                var nextRun = now.Date.AddHours(13);
                if (now >= nextRun)
                {
                    nextRun = nextRun.AddDays(1);
                }

                // Calculate delay until next run
                var delay = nextRun - now;
                _logger.LogInformation($"Next reminder email will be sent at {nextRun}");

                // Wait until next scheduled time
                await Task.Delay(delay, stoppingToken);

                // Execute the reminder
                using (var scope = _scopeFactory.CreateScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await emailService.SendEmailReminderAsync();
                    _logger.LogInformation("Reminder email sent successfully at {time}", DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled email sending.");
                // Wait for 5 minutes before retrying in case of error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
