using System;
using System.IO;
using System.Threading.Tasks;
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
using ClaimRequest.DAL.Data.Responses.Email;
using ClaimRequest.DAL.Data.Responses.Staff;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Repositories.Interfaces;

namespace ClaimRequest.UnitTest.Services
{
    public class EmailServiceTest
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IClaimService> _mockClaimService;
        private readonly Mock<IProjectService> _mockProjectService;
        private readonly Mock<IStaffService> _mockStaffService;
        private readonly Mock<IOtpService> _mockOtpService;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly OtpUtil _otpUtil;
        private readonly EmailService _emailService;

        public EmailServiceTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockClaimService = new Mock<IClaimService>();
            _mockProjectService = new Mock<IProjectService>();
            _mockStaffService = new Mock<IStaffService>();
            _mockOtpService = new Mock<IOtpService>();
            _mockLogger = new Mock<ILogger<EmailService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockOtpService = new Mock<IOtpService>();

            // Initialize OtpUtil if necessary
            _otpUtil = new OtpUtil(_mockConfiguration.Object);

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
    }
}