using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Responses.Project;
using ClaimRequest.DAL.Data.Responses.Staff;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.Service.Services.Implements
{
    public class EmailServiceTest
    {
        private readonly Mock<IClaimService> _mockClaimService;
        private readonly Mock<IProjectService> _mockProjectService;
        private readonly Mock<IStaffService> _mockStaffService;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly EmailService _emailService;

        public EmailServiceTest()
        {
            _mockClaimService = new Mock<IClaimService>();
            _mockProjectService = new Mock<IProjectService>();
            _mockStaffService = new Mock<IStaffService>();
            _mockLogger = new Mock<ILogger<EmailService>>();

            _emailService = new EmailService(
                Mock.Of<IConfiguration>(),
                _mockClaimService.Object,
                _mockLogger.Object,
                _mockProjectService.Object,
                _mockStaffService.Object
            );
        }

        [Fact]
        public async Task SendClaimReturnedEmail_Should_Throw_NotFoundException_When_Claim_Not_Found()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(service => service.AddEmailInfo(claimId))
                .ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendClaimReturnedEmail(claimId));
        }

        [Fact]
        public async Task SendManagerApprovedEmail_Should_Throw_Exception_When_Claim_Not_Found()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            _mockClaimService.Setup(service => service.AddEmailInfo(claimId))
                .ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _emailService.SendManagerApprovedEmail(approverId, claimId));
        }

        [Fact]
        public async Task SendClaimSubmittedEmail_Should_Throw_Exception_When_Claim_Not_Found()
        {
            // Arrange
            var claimerId = Guid.NewGuid();
            _mockClaimService.Setup(service => service.AddEmailInfo(claimerId))
                .ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _emailService.SendClaimSubmittedEmail(claimerId));
        }

        [Fact]
        public async Task SendClaimApprovedEmail_Should_Throw_NotFoundException_When_Claim_Not_Found()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            _mockClaimService.Setup(service => service.AddEmailInfo(claimId))
                .ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendClaimApprovedEmail(claimId));
        }

        [Fact]
        public async Task SendEmailAsync_Should_Throw_ArgumentException_When_Invalid_Email_Format()
        {
            // Arrange
            var recipientEmail = "invalid-email";
            var subject = "Test Subject";
            var body = "Test Body";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _emailService.SendEmailAsync(recipientEmail, subject, body));
        }
    }
}