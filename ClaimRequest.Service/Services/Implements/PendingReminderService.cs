using ClaimRequest.BLL.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClaimRequest.BLL.Services.Implements
{
    public class PendingReminderService : BackgroundService, IPendingReminderService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PendingReminderService> _logger;

        public PendingReminderService(IServiceScopeFactory serviceScopeFactory, ILogger<PendingReminderService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task SendReminderAsync()
        {
            await ExecuteAsync(CancellationToken.None);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PendingReminderService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var claimService = scope.ServiceProvider.GetRequiredService<IClaimService>();
                        var staffService = scope.ServiceProvider.GetRequiredService<IStaffService>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        var pendingClaims = await claimService.GetPendingClaimsAsync();
                        if (!pendingClaims.Any())
                        {
                            _logger.LogInformation("No pending claims found.");
                        }
                        else
                        {
                            var groupedClaims = pendingClaims.GroupBy(c => c.FinanceId);

                            foreach (var group in groupedClaims)
                            {
                                var financeId = group.Key;

                                if (financeId.HasValue)
                                {
                                    var financeStaff = await staffService.GetStaffById(financeId.Value);

                                    if (financeStaff != null && !string.IsNullOrEmpty(financeStaff.Email))
                                    {
                                        string recipientEmail = financeStaff.Email;
                                        string subject = "Reminder: Pending Claim Requests Need Approval";
                                        var updatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                                        string templatePath = Path.Combine(AppContext.BaseDirectory, "Services", "Templates", "PendingClaimReminder.html");
                                        string body = await File.ReadAllTextAsync(templatePath);

                                        string claimsList = string.Join("<br/>", group.Select(c => $"• Staff: {c.StaffName} - Project: {c.ProjectName}"));

                                        body = body.Replace("{FinanceName}", financeStaff.ResponseName)
                                                   .Replace("{ListName}", claimsList)
                                                   .Replace("{UpdatedDate}", updatedDate);

                                        await emailService.SendEmailAsync(recipientEmail, subject, body);

                                        _logger.LogInformation($"Sent reminder email to {recipientEmail}");
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"Finance staff with ID {financeId} not found or has no email.");
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("FinanceId is null, skipping.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while checking pending claims.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); 
            }

            _logger.LogInformation("PendingReminderService is stopping.");
        }
    }
}
