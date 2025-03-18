using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Email;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClaimRequest.BLL.Services.Implements
{
    public class PasswordReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PasswordReminderService> _logger;

        public PasswordReminderService(IServiceScopeFactory serviceScopeFactory, ILogger<PasswordReminderService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var enableReminder = Environment.GetEnvironmentVariable("ENABLE_PASSWORD_REMINDER");

            if (!string.Equals(enableReminder, "yes", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Password Reminder Service is disabled.");
                return; 
            }

            _logger.LogInformation("Password Reminder Service is enabled.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ClaimRequestDbContext>>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        var otpService = scope.ServiceProvider.GetRequiredService<IOtpService>();

                        DateTime timePasswordExpired = DateTime.UtcNow.AddMonths(-3);

                        var staffToRemind = await unitOfWork.GetRepository<Staff>().GetListAsync(
                            predicate: s => s.LastChangePassword == null || s.LastChangePassword <= timePasswordExpired
                        );

                        if (!staffToRemind.Any())
                        {
                            _logger.LogInformation("No staff members require password reminders.");
                        }
                        else
                        {
                            var semaphore = new SemaphoreSlim(10);
                            var tasks = staffToRemind.Select(async staff =>
                            {
                                await semaphore.WaitAsync();
                                try
                                {
                                    _logger.LogInformation($"Sending email to: {staff.Email}");

                                    var otp = OtpUtil.GenerateOtp(staff.Email);
                                    await otpService.CreateOtpEntity(staff.Email, otp);

                                    await emailService.SendEmailAsync(
                                        staff.Email,
                                        "Reminder: Change Your Password",
                                        $"Hi {staff.Name}, please update your password. OTP: {otp}"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Error sending email to {staff.Email}");
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            }).ToList();

                            await Task.WhenAll(tasks);
                            _logger.LogInformation("All password reminder emails sent successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending password reminder emails");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
