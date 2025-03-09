using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Responses.Claim;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

            _mockConfiguration.SetupGet(c => c["EmailSettings:Host"]).Returns("smtp.gmail.com");
            _mockConfiguration.SetupGet(c => c["EmailSettings:SmtpPort"]).Returns("587");
            _mockConfiguration.SetupGet(c => c["EmailSettings:SenderEmail"]).Returns("kosjapan391@gmail.com");
            _mockConfiguration.SetupGet(c => c["EmailSettings:SenderPassword"]).Returns("ykleedhszsgimlvu");

            _emailService = new EmailService(
                _mockConfiguration.Object,
                _mockClaimService.Object,
                _mockLogger.Object,
                _mockProjectService.Object,
                _mockStaffService.Object
            );
        }

        [Fact]
        public async Task SendEmailReminderAsync_ShouldNotSendEmail_WhenNoPendingClaims()
        {
            // Arrange
            _mockClaimService.Setup(service => service.GetPendingClaimsAsync())
                .ReturnsAsync(new List<ViewClaimResponse>());

            // Act
            await _emailService.SendEmailReminderAsync();

            // Assert
            _mockClaimService.Verify(service => service.GetPendingClaimsAsync(), Times.Once);
            _mockLogger.Verify(logger => logger.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendEmailReminderAsync_ShouldSendEmail_WhenPendingClaimsExist()
        {
            // Arrange
            var pendingClaims = new List<ViewClaimResponse>
            {
                new ViewClaimResponse 
                { 
                    StaffName = "Nguyễn Minh Thông",
                    ProjectName = "ClaimRequest"
                },
                new ViewClaimResponse 
                { 
                    StaffName = "Hoàng Ngọc Tiến",
                    ProjectName = "Alpha"
                }
            };
            
            _mockClaimService.Setup(service => service.GetPendingClaimsAsync())
                .ReturnsAsync(pendingClaims);

            // Act
            await _emailService.SendEmailReminderAsync();

            // Assert
            _mockClaimService.Verify(service => service.GetPendingClaimsAsync(), Times.Once);
            _mockLogger.Verify(logger => logger.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendEmailReminderAsync_ShouldLogError_WhenExceptionOccurs()
        {
            // Arrange
            _mockClaimService.Setup(service => service.GetPendingClaimsAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await Assert.ThrowsAsync<Exception>(() => _emailService.SendEmailReminderAsync());

            // Assert
            _mockLogger.Verify(
                x => x.LogError(
                    It.IsAny<Exception>(),
                    "Error sending claim reminder email"
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task SendClaimReturnedEmail_ShouldThrowNotFoundException_WhenClaimNotFound()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(service => service.GetClaimById(claimId))
                .ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendClaimReturnedEmail(claimId));
        }

        [Fact]
        public async Task SendClaimReturnedEmail_ShouldSendEmail_WhenClaimExists()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claim = new Claim 
            { 
                Id = claimId,
                Claimer = new Staff { Name = "Nguyễn Minh Thông", Email = "thongnmse172317@fpt.edu.vn" },
                Project = new Project { Name = "claimrequest" },
                Status = ClaimStatus.Draft,
                UpdateAt = DateTime.UtcNow
            };

            _mockClaimService.Setup(service => service.GetClaimById(claimId))
                .ReturnsAsync(claim);

            // Act
            await _emailService.SendClaimReturnedEmail(claimId);

            // Assert
            _mockClaimService.Verify(service => service.GetClaimById(claimId), Times.Once);
            _mockLogger.Verify(logger => logger.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendClaimReturnedEmail_ShouldLogError_WhenExceptionOccurs()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(service => service.GetClaimById(claimId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _emailService.SendClaimReturnedEmail(claimId));
            _mockLogger.Verify(
                x => x.LogError(
                    It.IsAny<Exception>(),
                    It.IsAny<string>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task SendClaimSubmittedEmail_ShouldThrowNotFoundException_WhenClaimNotFound()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(service => service.GetClaimById(claimId))
                .ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendClaimSubmittedEmail(claimId));
        }

        [Fact]
        public async Task SendManagerApprovedEmail_ShouldThrowNotFoundException_WhenClaimNotFound()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(service => service.GetClaimById(claimId))
                .ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendManagerApprovedEmail(claimId));
        }
    }
}