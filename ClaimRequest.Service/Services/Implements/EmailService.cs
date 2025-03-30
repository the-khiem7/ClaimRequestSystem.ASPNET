#define SMTP
//#define OAUTH
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using System.Net.Mail;
using System.Net;
using ClaimRequest.DAL.Data.Responses.Project;
using ClaimRequest.DAL.Data.Responses.Staff;
using ClaimRequest.DAL.Data.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ClaimRequest.DAL.Data.Responses.Email;
using ClaimRequest.DAL.Data.Requests.Email;
using Google.Apis.Gmail.v1;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Repositories.Interfaces;


namespace ClaimRequest.BLL.Services.Implements
{
    public class EmailService : IEmailService
    {
        private static readonly string[] Scopes = { GmailService.Scope.GmailSend };
        private readonly string _applicationName;
        private readonly string _senderEmail;
        public readonly IClaimService _claimService;
        public readonly IProjectService _projectService;
        public readonly IStaffService _staffService;
        public readonly ILogger _logger;
        public readonly IUnitOfWork _unitOfWork;
        public readonly OtpUtil _otpUtil;
        public readonly IOtpService _otpService;

        public readonly string _smtpServer;
        public readonly int _port;
        public readonly string _password;

        public EmailService(IUnitOfWork<ClaimRequestDbContext> unitOfWork, IConfiguration configuration, IClaimService claimService, ILogger<EmailService> logger, IProjectService projectService, IStaffService staffService, IOtpService otpService, OtpUtil otpUtil)
        {
#if OAUTH
            _senderEmail = configuration["EmailSettings:SenderEmailOauth"];
#endif
#if SMTP
            _senderEmail = configuration["EmailSettings:SenderEmailSMTP"];
#endif
            _smtpServer = configuration["EmailSettings:Host"];
            _port = int.Parse(configuration["EmailSettings:SmtpPort"]);
            _password = configuration["EmailSettings:SenderPassword"];

            _claimService = claimService;
            _projectService = projectService;
            _staffService = staffService;
            _logger = logger;
            _otpService = otpService;
            _unitOfWork = unitOfWork;
            _otpUtil = otpUtil;
        }



        public async Task SendClaimReturnedEmail(Guid Id)
        {
            try
            {
                var claim = await _claimService.AddEmailInfo(Id);
                if (claim == null)
                    throw new NotFoundException($"Claim with {Id} not found.");

                string projectName = claim.Project.Name;

                var updatedDate = claim.UpdateAt.ToString("yyyy-MM-dd HH:mm:ss");

                var claimer = claim.Claimer;

                string recipientEmail = claimer.Email;
                string subject = $"Claim Request for {projectName} - {claimer.Name} ({claimer.Id})";


                string templatePath = Path.Combine(AppContext.BaseDirectory, "Services", "Templates", "ClaimReturnedEmailTemplate.html");
                string body = await File.ReadAllTextAsync(templatePath);

                body = body.Replace("{ClaimerName}", claimer.Name)
                           .Replace("{ProjectName}", projectName)
                           .Replace("{ClaimerId}", claimer.Id.ToString())
                           .Replace("{UpdatedDate}", updatedDate);

                await SendEmailAsync(recipientEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendClaimReturnedEmail: {ex.Message}");
                throw;
            }
        }

        public async Task SendManagerApprovedEmail( Guid Id)
        {
            try
            {
                Claim claim = await _claimService.AddEmailInfo(Id);
                if (claim == null)
                    throw new Exception("Claim not found.");

                string projectName = claim.Project.Name;

                var updatedDate = claim.UpdateAt.ToString("yyyy-MM-dd HH:mm:ss");

                CreateStaffResponse claimer = await _staffService.GetStaffById(claim.ClaimerId);

                // Get finance staff using FinanceId from claim
                if (!claim.FinanceId.HasValue)
                    throw new Exception("Finance staff not assigned to this claim.");

                var financeStaff = await _staffService.GetStaffById(claim.FinanceId.Value);
                if (financeStaff == null || string.IsNullOrEmpty(financeStaff.Email))
                    throw new Exception("Finance staff not found or email is invalid.");

                string recipientEmail = financeStaff.Email;
                string subject = $"Claim Request for {projectName} - {claimer.ResponseName} ({claimer.Id})";

                string templatePath = Path.Combine(AppContext.BaseDirectory, "Services", "Templates", "ManagerApprovedEmailTemplate.html");
                string body = await File.ReadAllTextAsync(templatePath);

                body = body.Replace("{ClaimerName}", claimer.ResponseName)
                           .Replace("{ProjectName}", projectName)
                           .Replace("{ClaimerId}", claimer.Id.ToString())
                           .Replace("{UpdatedDate}", updatedDate);

                await SendEmailAsync(recipientEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending claim returned email with claimId: {claimId}", Id);
                throw;
            }
        }

        public async Task SendClaimSubmittedEmail(Guid Id)
        {
            try
            {
                Claim claim = await _claimService.AddEmailInfo(Id);
                if (claim == null)
                    throw new Exception("Claim not found.");

                CreateProjectResponse project = await _projectService.GetProjectById(claim.ProjectId);
                if (project == null)
                    throw new Exception("Project not found.");

                string projectName = claim.Project.Name;

                string projectManagerName = project.ProjectManager.ResponseName;
                string projectManagerEmail = project.ProjectManager.Email;

                var updatedDate = claim.UpdateAt.ToString("yyyy-MM-dd HH:mm:ss");

                CreateStaffResponse claimer = await _staffService.GetStaffById(claim.ClaimerId);
                if (claimer == null || string.IsNullOrEmpty(claimer.Email))
                    throw new Exception("Claimer information is invalid.");

                string recipientEmail = projectManagerEmail;
                string subject = $"Claim Request for {projectName} - {claimer.ResponseName} ({claimer.Id})";


                string templatePath = Path.Combine(AppContext.BaseDirectory, "Services", "Templates", "ClaimSubmittedEmailTemplate.html");
                string body = await File.ReadAllTextAsync(templatePath);

                body = body.Replace("ProjectManagerName", projectManagerName)
                            .Replace("{ClaimerName}", claimer.ResponseName)
                           .Replace("{ProjectName}", projectName)
                           .Replace("{ClaimerId}", claimer.Id.ToString())
                           .Replace("{UpdatedDate}", updatedDate);

                await SendEmailAsync(recipientEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendClaimReturnedEmail: {ex.Message}");
                throw;
            }
        }

        public async Task SendClaimApprovedEmail(Guid Id)
        {
            try
            {
                Claim claim = await _claimService.AddEmailInfo(Id);
                if (claim == null)
                    throw new NotFoundException($"Claim with {Id} not found.");

                string projectName = claim.Project.Name;

                var updatedDate = claim.UpdateAt.ToString("yyyy-MM-dd HH:mm:ss");

                var claimer = claim.Claimer;

                string recipientEmail = claimer.Email;
                string subject = $"Claim Request for {projectName} - {claimer.Name} ({claimer.Id})";


                string templatePath = Path.Combine(AppContext.BaseDirectory, "Services", "Templates", "ClaimApprovedEmailTemplate.html");
                string body = await File.ReadAllTextAsync(templatePath);

                body = body.Replace("{ClaimerName}", claimer.Name)
                           .Replace("{ProjectName}", projectName)
                           .Replace("{ClaimerId}", claimer.Id.ToString())
                           .Replace("{UpdatedDate}", updatedDate);

                await SendEmailAsync(recipientEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendClaimReturnedEmail: {ex.Message}");
                throw;
            }
        }
        public virtual async Task SendEmailAsync(string recipientEmail, string subject, string body)
        {
#if OAUTH
            try
            {
                // Validate email format
                try
                {
                    var mailAddress = new MailAddress(recipientEmail);
                }
                catch (FormatException)
                {
                    throw new ArgumentException("Invalid email format", nameof(recipientEmail));
                }

                UserCredential credential;
                using (var stream = new FileStream("client_secrect.json", FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore("token.json", true));
                }

                // Create Gmail API service.
                var service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Claim Request System"
                });

                // Create the email message
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("noreply@emailservice.com", _senderEmail));
                emailMessage.To.Add(new MailboxAddress("", recipientEmail));
                emailMessage.Subject = subject;

                // Create the HTML body
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body,
                    TextBody = "This is the plain text version of the email body."
                };
                emailMessage.Body = bodyBuilder.ToMessageBody();

                // Convert to Gmail API format
                using (var memoryStream = new MemoryStream())
                {
                    emailMessage.WriteTo(memoryStream);
                    var rawMessage = Convert.ToBase64String(memoryStream.ToArray())
                        .Replace('+', '-')
                        .Replace('/', '_')
                        .Replace("=", "");

                    var message = new Message { Raw = rawMessage };

                    try
                    {
                        await service.Users.Messages.Send(message, "me").ExecuteAsync();
                    }
                    catch (GoogleApiException ex)
                    {
                        _logger.LogError(ex, $"Google API error: {ex.Message}");
                        _logger.LogError($"Error details: {ex.Error?.Message}");
                        _logger.LogError($"Error code: {ex.Error?.Code}");
                        _logger.LogError($"Error errors: {string.Join(", ", ex.Error?.Errors.Select(e => e.Message))}");
                        throw;
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SendEmailAsync: {ex.Message}");
                throw;
            }
#endif
#if SMTP
            try
            {
                using (var smtpClient = new SmtpClient(_smtpServer, _port))
                {
                    smtpClient.Credentials = new NetworkCredential(_senderEmail, _password);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_senderEmail, "noreply@emailservice.com"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                    };
                    mailMessage.To.Add(recipientEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {recipientEmail}", recipientEmail);
                throw;
            }

#endif
        }
        public async Task<SendOtpEmailResponse> SendOtpEmailAsync(SendOtpEmailRequest request)
        {
            var response = new SendOtpEmailResponse();
            try
            {
                var existingStaff = await _unitOfWork.GetRepository<Staff>().SingleOrDefaultAsync(predicate: s => s.Email == request.Email);
                if (existingStaff == null)
                {
                    throw new NotFoundException($"Staff with email {request.Email} not found.");
                }

                var otp = _otpUtil.GenerateOtp(request.Email);
                await _otpService.CreateOtpEntity(request.Email, otp);

                string templatePath = Path.Combine(AppContext.BaseDirectory, "Services", "Templates", "OtpEmailTemplate.html");
                string body = await File.ReadAllTextAsync(templatePath);

                body = body.Replace("{OtpCode}", otp)
                           .Replace("{ExpiryTime}", "5");

                string recipientEmail = request.Email;
                string subject = "Your OTP Code";

                await SendEmailAsync(recipientEmail, subject, body);
                response.Success = true;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email.");
                response.Success = false;
                throw;
            }

            return response;
        }
    }
}

