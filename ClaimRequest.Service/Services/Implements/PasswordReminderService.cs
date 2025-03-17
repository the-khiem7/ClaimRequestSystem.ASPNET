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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ClaimRequestDbContext>>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        var otpService = scope.ServiceProvider.GetRequiredService<IOtpService>(); 

                        DateTime timePasswordExpired = DateTime.UtcNow.AddHours(-3);

                        // Lấy danh sách staff cần remind
                        // Chỉ lấy staff có LastChangePassword là null hoặc có thời gian đổi mật khẩu
                        // so với hiện tại đã 3 tiếng trước
                        var staffToRemind = await unitOfWork.GetRepository<Staff>().GetListAsync(
                            predicate: s => s.LastChangePassword != null && s.LastChangePassword <= timePasswordExpired
                        );

                        var semaphore = new SemaphoreSlim(10);
                        var tasks = new List<Task>();

                        foreach (var staff in staffToRemind)
                        {
                            _logger.LogInformation($"Sending email to: {staff.Email}, LastChangePassword: {staff.LastChangePassword}");
                            var otp = OtpUtil.GenerateOtp(staff.Email);
                            await otpService.CreateOtpEntity(staff.Email, otp); 

                            await semaphore.WaitAsync();
                            tasks.Add(Task.Run(async () =>
                            {
                                try
                                {
                                    await emailService.SendEmailAsync(
                                        staff.Email,
                                        "Reminder: Change Your Password",
                                        $"Hi {staff.Name}, you haven't changed your password for a while. For security reasons, please update it. Here is your OTP to proceed: {otp}"
                                    );
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            }));
                        }

                        await Task.WhenAll(tasks);
                        _logger.LogInformation("All password reminder emails sent successfully.");
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
