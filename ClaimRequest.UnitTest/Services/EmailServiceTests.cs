using System;
using System.IO;
using System.Threading.Tasks;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Responses.Project;
using ClaimRequest.DAL.Data.Responses.Staff;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<IClaimService> _mockClaimService;
        private readonly Mock<IProjectService> _mockProjectService;
        private readonly Mock<IStaffService> _mockStaffService;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly EmailService _emailService;

        public EmailServiceTests()
        {
            _mockClaimService = new Mock<IClaimService>();
            _mockProjectService = new Mock<IProjectService>();
            _mockStaffService = new Mock<IStaffService>();
            _mockLogger = new Mock<ILogger<EmailService>>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.SetupGet(c => c["EmailSettings:Host"]).Returns("smtp.test.com");
            _mockConfiguration.SetupGet(c => c["EmailSettings:SmtpPort"]).Returns("587");
            _mockConfiguration.SetupGet(c => c["EmailSettings:SenderEmail"]).Returns("sender@test.com");
            _mockConfiguration.SetupGet(c => c["EmailSettings:SenderPassword"]).Returns("password");

            _emailService = new EmailService(
                _mockConfiguration.Object,
                _mockClaimService.Object,
                _mockLogger.Object,
                _mockProjectService.Object,
                _mockStaffService.Object
            );
        }

        [Fact]
        public async Task SendEmailAsync_ShouldSendEmail_WhenParametersAreValid()
        {
            // Arrange
            var recipientEmail = "test@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            // Act
            await _emailService.SendEmailAsync(recipientEmail, subject, body);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error sending email")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Never);
        }

        [Fact]
        public async Task SendClaimReturnedEmail_ShouldSendEmail_WhenClaimIsValid()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claim = new Claim
            {
                Id = claimId,
                Project = new Project { Name = "Test Project" },
                UpdateAt = DateTime.UtcNow,
                Claimer = new Staff { Name = "Test Claimer", Email = "claimer@test.com" }
            };

            _mockClaimService.Setup(s => s.AddEmailInfo(claimId)).ReturnsAsync(claim);

            // Act
            await _emailService.SendClaimReturnedEmail(claimId);

            // Assert
            _mockClaimService.Verify(s => s.AddEmailInfo(claimId), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error sending email")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Never);
        }

        [Fact]
        public async Task SendClaimReturnedEmail_ShouldThrowException_WhenClaimNotFound()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(s => s.AddEmailInfo(claimId)).ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendClaimReturnedEmail(claimId));
        }

        [Fact]
        public async Task SendClaimSubmittedEmail_ShouldSendEmail_WhenClaimIsValid()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claim = new Claim
            {
                Id = claimId,
                ProjectId = Guid.NewGuid(),
                Project = new Project { Name = "Test Project" },
                UpdateAt = DateTime.UtcNow,
                Claimer = new Staff { Name = "Test Claimer", Email = "claimer@test.com" }
            };

            var projectResponse = new CreateProjectResponse
            {
                ProjectManager = new CreateStaffResponse
                {
                    ResponseName = "Test Manager",
                    Email = "manager@test.com"
                }
            };

            _mockClaimService.Setup(s => s.AddEmailInfo(claimId)).ReturnsAsync(claim);
            _mockProjectService.Setup(s => s.GetProjectById(claim.ProjectId)).ReturnsAsync(projectResponse);

            // Act
            await _emailService.SendClaimSubmittedEmail(claimId);

            // Assert
            _mockClaimService.Verify(s => s.AddEmailInfo(claimId), Times.Once);
            _mockProjectService.Verify(s => s.GetProjectById(claim.ProjectId), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error sending email")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Never);
        }

        [Fact]
        public async Task SendClaimSubmittedEmail_ShouldThrowException_WhenProjectNotFound()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claim = new Claim
            {
                Id = claimId,
                ProjectId = Guid.NewGuid(),
                Project = new Project { Name = "Test Project" },
                UpdateAt = DateTime.UtcNow,
                Claimer = new Staff { Name = "Test Claimer", Email = "claimer@test.com" }
            };

            _mockClaimService.Setup(s => s.AddEmailInfo(claimId)).ReturnsAsync(claim);
            _mockProjectService.Setup(s => s.GetProjectById(claim.ProjectId)).ReturnsAsync((CreateProjectResponse)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _emailService.SendClaimSubmittedEmail(claimId));
            Assert.Equal("Project not found.", exception.Message);
        }

        [Fact]
        public async Task SendManagerApprovedEmail_ShouldSendEmail_WhenClaimIsValid()
        {
            // Arrange
            var approverId = Guid.NewGuid();
            var claimId = Guid.NewGuid();
            var financeId = Guid.NewGuid();
            var claim = new Claim
            {
                Id = claimId,
                Project = new Project { Name = "Test Project" },
                UpdateAt = DateTime.UtcNow,
                ClaimerId = Guid.NewGuid(),
                FinanceId = financeId
            };

            var claimer = new CreateStaffResponse
            {
                Id = claim.ClaimerId,
                ResponseName = "Test Claimer"
            };

            var financeStaff = new CreateStaffResponse
            {
                Id = financeId,
                Email = "finance@test.com"
            };

            _mockClaimService.Setup(s => s.AddEmailInfo(claimId)).ReturnsAsync(claim);
            _mockStaffService.Setup(s => s.GetStaffById(claim.ClaimerId)).ReturnsAsync(claimer);
            _mockStaffService.Setup(s => s.GetStaffById(financeId)).ReturnsAsync(financeStaff);

            // Act
            await _emailService.SendManagerApprovedEmail(approverId, claimId);

            // Assert
            _mockClaimService.Verify(s => s.AddEmailInfo(claimId), Times.Once);
            _mockStaffService.Verify(s => s.GetStaffById(claim.ClaimerId), Times.Once);
            _mockStaffService.Verify(s => s.GetStaffById(financeId), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error sending email")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Never);
        }

        [Fact]
        public async Task SendManagerApprovedEmail_ShouldThrowException_WhenFinanceStaffNotAssigned()
        {
            // Arrange
            var approverId = Guid.NewGuid();
            var claimId = Guid.NewGuid();
            var claim = new Claim
            {
                Id = claimId,
                Project = new Project { Name = "Test Project" },
                UpdateAt = DateTime.UtcNow,
                ClaimerId = Guid.NewGuid(),
                FinanceId = null
            };

            _mockClaimService.Setup(s => s.AddEmailInfo(claimId)).ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => 
                _emailService.SendManagerApprovedEmail(approverId, claimId));
            Assert.Equal("Finance staff not assigned to this claim.", exception.Message);
        }

        [Fact]
        public async Task SendClaimApprovedEmail_ShouldSendEmail_WhenClaimIsValid()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claim = new Claim
            {
                Id = claimId,
                Project = new Project { Name = "Test Project" },
                UpdateAt = DateTime.UtcNow,
                Claimer = new Staff { Name = "Test Claimer", Email = "claimer@test.com" }
            };

            _mockClaimService.Setup(s => s.AddEmailInfo(claimId)).ReturnsAsync(claim);

            // Act
            await _emailService.SendClaimApprovedEmail(claimId);

            // Assert
            _mockClaimService.Verify(s => s.AddEmailInfo(claimId), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error sending email")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Never);
        }

        [Fact]
        public async Task SendClaimApprovedEmail_ShouldThrowException_WhenClaimNotFound()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(s => s.AddEmailInfo(claimId)).ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendClaimApprovedEmail(claimId));
        }
    }
}
