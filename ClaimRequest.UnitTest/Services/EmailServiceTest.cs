using System.Linq.Expressions;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Repositories.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Email;
using ClaimRequest.DAL.Data.Responses.Staff;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Responses.Project;

namespace ClaimRequest.UnitTest.Services
{
    public class EmailServiceTest
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<IClaimService> _mockClaimService;
        private readonly Mock<IProjectService> _mockProjectService;
        private readonly Mock<IStaffService> _mockStaffService;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IOtpService> _mockOtpService;
        private readonly OtpUtil _otpUtil;
        private readonly EmailService _emailService;

        public EmailServiceTest()
        {
            // Initialize all mocks
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockClaimService = new Mock<IClaimService>();
            _mockProjectService = new Mock<IProjectService>();
            _mockStaffService = new Mock<IStaffService>();
            _mockLogger = new Mock<ILogger<EmailService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockOtpService = new Mock<IOtpService>();

            // Setup SMTP settings
            var smtpSection = new Mock<IConfigurationSection>();
            smtpSection.Setup(x => x["Host"]).Returns("smtp.test.com");
            smtpSection.Setup(x => x["Port"]).Returns("587");
            smtpSection.Setup(x => x["Username"]).Returns("test@test.com");
            smtpSection.Setup(x => x["Password"]).Returns("test-password");
            smtpSection.Setup(x => x["EnableSsl"]).Returns("true");
            _mockConfiguration.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);

            // Setup Email settings
            _mockConfiguration.Setup(c => c["EmailSettings:SenderEmailSMTP"]).Returns("test@example.com");
            _mockConfiguration.Setup(c => c["EmailSettings:Host"]).Returns("smtp.example.com");
            _mockConfiguration.Setup(c => c["EmailSettings:SmtpPort"]).Returns("587");
            _mockConfiguration.Setup(c => c["EmailSettings:SenderPassword"]).Returns("password");

            // Create OtpUtil instance
            _otpUtil = new OtpUtil(_mockConfiguration.Object);

            // Create EmailService instance
            _emailService = new EmailService(
                _mockUnitOfWork.Object,
                _mockConfiguration.Object,
                _mockClaimService.Object,
                _mockLogger.Object,
                _mockProjectService.Object,
                _mockStaffService.Object,
                _mockOtpService.Object,
                _otpUtil
            );

            // Mock SendEmailAsync method
            var emailServiceMock = new Mock<EmailService>(
                _mockUnitOfWork.Object,
                _mockConfiguration.Object,
                _mockClaimService.Object,
                _mockLogger.Object,
                _mockProjectService.Object,
                _mockStaffService.Object,
                _mockOtpService.Object,
                _otpUtil
            ) { CallBase = true };

            emailServiceMock.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _emailService = emailServiceMock.Object;
        }

        [Fact]
        public async Task SendClaimReturnedEmail_WhenClaimNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(c => c.AddEmailInfo(claimId)).ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendClaimReturnedEmail(claimId));
        }

        [Fact]
        public async Task SendClaimApprovedEmail_WhenClaimNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(c => c.AddEmailInfo(claimId)).ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendClaimApprovedEmail(claimId));
        }

        [Fact]
        public async Task SendClaimSubmittedEmail_WhenClaimNotFound_ThrowsException()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(c => c.AddEmailInfo(claimId)).ReturnsAsync((Claim)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _emailService.SendClaimSubmittedEmail(claimId));
            Assert.Equal("Claim not found.", exception.Message);
        }

        [Fact]
        public async Task SendOtpEmailAsync_WhenStaffNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var request = new SendOtpEmailRequest { Email = "nonexistent@example.com" };
            _mockUnitOfWork.Setup(u => u.GetRepository<Staff>().SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync((Staff)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendOtpEmailAsync(request));
        }

        [Fact]
        public async Task SendClaimSubmittedEmail_ValidClaim_SendsEmail()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claim = new Claim
            {
                Id = claimId,
                ProjectId = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                UpdateAt = DateTime.UtcNow,
                Project = new Project { Name = "Test Project" },
                Claimer = new Staff { Name = "Test User", Email = "test@example.com" }
            };

            var project = new CreateProjectResponse
            {
                ProjectManager = new CreateStaffResponse
                {
                    ResponseName = "Project Manager",
                    Email = "manager@example.com"
                }
            };

            _mockClaimService.Setup(x => x.AddEmailInfo(claimId)).ReturnsAsync(claim);
            _mockProjectService.Setup(x => x.GetProjectById(claim.ProjectId)).ReturnsAsync(project);
            _mockStaffService.Setup(x => x.GetStaffById(claim.ClaimerId)).ReturnsAsync(new CreateStaffResponse 
            { 
                ResponseName = "Test User",
                Email = "test@example.com"
            });

            // Act
            await _emailService.SendClaimSubmittedEmail(claimId);

            // Assert
            _mockClaimService.Verify(x => x.AddEmailInfo(claimId), Times.Once);
            _mockProjectService.Verify(x => x.GetProjectById(claim.ProjectId), Times.Once);
            _mockStaffService.Verify(x => x.GetStaffById(claim.ClaimerId), Times.Once);
        }

        [Fact]
        public async Task SendManagerApprovedEmail_ValidClaim_SendsEmail()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claim = new Claim
            {
                Id = claimId,
                ProjectId = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                FinanceId = Guid.NewGuid(),
                UpdateAt = DateTime.UtcNow,
                Project = new Project { Name = "Test Project" }
            };

            _mockClaimService.Setup(x => x.AddEmailInfo(claimId)).ReturnsAsync(claim);
            _mockStaffService.Setup(x => x.GetStaffById(claim.ClaimerId)).ReturnsAsync(new CreateStaffResponse 
            { 
                ResponseName = "Test User",
                Id = claim.ClaimerId
            });
            _mockStaffService.Setup(x => x.GetStaffById(claim.FinanceId.Value)).ReturnsAsync(new CreateStaffResponse 
            { 
                Email = "finance@example.com"
            });

            // Act
            await _emailService.SendManagerApprovedEmail(claimId);

            // Assert
            _mockClaimService.Verify(x => x.AddEmailInfo(claimId), Times.Once);
            _mockStaffService.Verify(x => x.GetStaffById(claim.ClaimerId), Times.Once);
            _mockStaffService.Verify(x => x.GetStaffById(claim.FinanceId.Value), Times.Once);
        }

        [Fact]
        public async Task SendClaimReturnedEmail_ValidClaim_SendsEmail()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claim = new Claim
            {
                Id = claimId,
                ProjectId = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                UpdateAt = DateTime.UtcNow,
                Project = new Project { Name = "Test Project" },
                Claimer = new Staff { Name = "Test User", Email = "test@example.com" }
            };

            _mockClaimService.Setup(x => x.AddEmailInfo(claimId)).ReturnsAsync(claim);

            // Act
            await _emailService.SendClaimReturnedEmail(claimId);

            // Assert
            _mockClaimService.Verify(x => x.AddEmailInfo(claimId), Times.Once);
        }

        [Fact]
        public async Task SendClaimSubmittedEmail_InvalidClaim_ThrowsException()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(x => x.AddEmailInfo(claimId)).ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _emailService.SendClaimSubmittedEmail(claimId));
        }

        [Fact]
        public async Task SendManagerApprovedEmail_InvalidClaim_ThrowsException()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(x => x.AddEmailInfo(claimId)).ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _emailService.SendManagerApprovedEmail(claimId));
        }

        [Fact]
        public async Task SendClaimReturnedEmail_InvalidClaim_ThrowsException()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(x => x.AddEmailInfo(claimId)).ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendClaimReturnedEmail(claimId));
        }

        [Fact]
        public async Task SendManagerApprovedEmail_NoFinanceStaff_ThrowsException()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claim = new Claim
            {
                Id = claimId,
                ProjectId = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                UpdateAt = DateTime.UtcNow,
                Project = new Project { Name = "Test Project" }
            };

            _mockClaimService.Setup(x => x.AddEmailInfo(claimId)).ReturnsAsync(claim);
            _mockStaffService.Setup(x => x.GetStaffById(claim.ClaimerId)).ReturnsAsync(new CreateStaffResponse 
            { 
                ResponseName = "Test User",
                Id = claim.ClaimerId
            });

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _emailService.SendManagerApprovedEmail(claimId));
        }
    }
}