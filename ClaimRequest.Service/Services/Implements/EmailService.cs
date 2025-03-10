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


namespace ClaimRequest.BLL.Services.Implements
{
    public class EmailService : IEmailService    
    {
        public readonly string _smtpServer;
        public readonly int _port;
        public readonly string _senderEmail;
        public readonly string _password;
        public readonly IClaimService _claimService;
        public readonly IProjectService _projectService;
        public readonly IStaffService _staffService;

        public readonly ILogger _logger;

        public EmailService(IConfiguration configuration, IClaimService claimService, ILogger<EmailService> logger, IProjectService projectService, IStaffService staffService)
        {
            _smtpServer = configuration["EmailSettings:Host"];
            _port = int.Parse(configuration["EmailSettings:SmtpPort"]);
            _senderEmail = configuration["EmailSettings:SenderEmail"];
            _password = configuration["EmailSettings:SenderPassword"];
            _claimService = claimService;
            _projectService = projectService;
            _staffService = staffService;
            _logger = logger;

        }

        public async Task SendEmailReminderAsync()
        {
            try
            {
                var pendingClaims = await _claimService.GetPendingClaimsAsync();
                if (!pendingClaims.Any()) return; // Không có claim nào Pending thì không gửi

                string recipientEmail = "thongnmse172317@fpt.edu.vn";
                string subject = "Reminder: Pending Claim Requests";
                var updatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                // Đọc template email
                string templatePath = Path.Combine(AppContext.BaseDirectory, "Services", "Templates", "ClaimReminderTemplate.html");
                string body = await File.ReadAllTextAsync(templatePath);

                // Danh sách claim Pending
                string claimsList = string.Join("<br/>", pendingClaims.Select(c => $"• Staff: {c.StaffName} - Project: {c.ProjectName}"));

                // Thay thế placeholder trong template
                body = body.Replace("{ClaimerName}", "Approver")
                           .Replace("{ListName}", claimsList)
                           .Replace("{UpdatedDate}", updatedDate);

                await SendEmailAsync(recipientEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending claim reminder email");
                throw;
            }
        }



        public async Task SendClaimReturnedEmail(Guid Id)
        {
            try
            {
                var claim = await _claimService.GetClaimById(Id);
                if (claim == null)
                    throw new NotFoundException($"Claim with { Id} not found.");

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
                Claim claim = await _claimService.GetClaimById(claimId);
                if (claim == null)
                    throw new Exception("Claim not found.");



                string projectName = claim.Project.Name;

                var updatedDate = claim.UpdateAt.ToString("yyyy-MM-dd HH:mm:ss");   

                CreateStaffResponse claimer = await _staffService.GetStaffById(claim.ClaimerId);
                string recipientEmail = claimer.Email;
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

        public async Task SendClaimSubmittedEmail(Guid claimId)
        {
            try
            {
                Claim claim = await _claimService.GetClaimById(claimId);
                if (claim == null)
                    throw new Exception("Claim not found.");

                CreateProjectResponse project = await _projectService.GetProjectById(claim.ProjectId);
                if (project == null)
                    throw new Exception("Project not found.");

                string projectName = claim.Project.Name;

                string projectManagerName = claim.Claimer.Name;
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
                Claim claim = await _claimService.GetClaimById(claimId);
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
        }

        public Task SendEmailAsync(Guid claimId)
        {
            throw new NotImplementedException();
        }
    }
}
