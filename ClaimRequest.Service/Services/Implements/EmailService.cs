using ClaimRequest.BLL.Services.Interfaces;
using MailKit.Security;
using ClaimRequest.DAL.Data.Entities;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Responses.Project;
using ClaimRequest.DAL.Data.Responses.Staff;
using ClaimRequest.DAL.Data.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClaimRequest.DAL.Data.Responses.Email;
using ClaimRequest.DAL.Data.Requests.Email;
using MimeKit;
using static System.Formats.Asn1.AsnWriter;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Auth0.ManagementApi.Models;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Gmail.v1.Data;
using Google;


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

        public EmailService(IConfiguration configuration, IClaimService claimService, ILogger<EmailService> logger, IProjectService projectService, IStaffService staffService)
        {
            _senderEmail = configuration["EmailSettings:SenderEmail"];
            _claimService = claimService;
            _projectService = projectService;
            _staffService = staffService;
            _logger = logger;
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

        public async Task SendManagerApprovedEmail(Guid approverId, Guid claimId)
        {
            try
            {
                Claim claim = await _claimService.AddEmailInfo(claimId);
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
                _logger.LogError(ex, "Error sending claim returned email with claimId: {claimId}", claimId);
                throw;
            }
        }

        public async Task SendClaimSubmittedEmail(Guid claimerId)
        {
            try
            {
                Claim claim = await _claimService.AddEmailInfo(claimerId);
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

        public async Task SendClaimApprovedEmail(Guid claimId)
        {
            try
            {
                Claim claim = await _claimService.AddEmailInfo(claimId);
                if (claim == null)
                    throw new NotFoundException($"Claim with {claimId} not found.");

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
        public async Task SendEmailAsync(string recipientEmail, string subject, string body)
        {
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
        }


    }
}
