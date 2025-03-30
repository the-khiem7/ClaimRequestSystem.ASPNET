using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Email;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class SendOtpServiceTest
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IClaimService> _mockClaimService;
        private readonly Mock<IProjectService> _mockProjectService;
        private readonly Mock<IStaffService> _mockStaffService;
        private readonly Mock<IOtpService> _mockOtpService;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly Mock<IGenericRepository<Staff>> _mockStaffRepository;
        private readonly EmailService _emailService;

        public SendOtpServiceTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockClaimService = new Mock<IClaimService>();
            _mockProjectService = new Mock<IProjectService>();
            _mockStaffService = new Mock<IStaffService>();
            _mockOtpService = new Mock<IOtpService>();
            _mockLogger = new Mock<ILogger<EmailService>>();
            _mockStaffRepository = new Mock<IGenericRepository<Staff>>();

            _mockConfiguration.Setup(c => c["OtpSettings:SecretSalt"]).Returns("TestSalt123");

            _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>())
                .Returns(_mockStaffRepository.Object);

            _mockConfiguration.Setup(c => c["EmailSettings:SenderEmailSMTP"]).Returns("test@example.com");
            _mockConfiguration.Setup(c => c["EmailSettings:Host"]).Returns("smtp.example.com");
            _mockConfiguration.Setup(c => c["EmailSettings:SmtpPort"]).Returns("587");
            _mockConfiguration.Setup(c => c["EmailSettings:SenderPassword"]).Returns("testPassword");

            var mockOtpUtil = new Mock<OtpUtil>(_mockConfiguration.Object);
            mockOtpUtil.Setup(u => u.GenerateOtp(It.IsAny<string>())).Returns("123456");

            _emailService = new MockedEmailService(
                _mockUnitOfWork.Object,
                _mockConfiguration.Object,
                _mockClaimService.Object,
                _mockLogger.Object,
                _mockProjectService.Object,
                _mockStaffService.Object,
                _mockOtpService.Object,
                mockOtpUtil.Object
            );
        }

        [Fact]
        public async Task SendOtpEmailAsync_ValidRequest_ReturnsSuccessResponse()
        {
            var email = "test@example.com";
            var request = new SendOtpEmailRequest { Email = email };
            var staff = new Staff { Id = Guid.NewGuid(), Email = email, Name = "Test User" };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync(staff);

            _mockOtpService.Setup(s => s.CreateOtpEntity(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _emailService.SendOtpEmailAsync(request);

            Assert.NotNull(result);
            Assert.True(result.Success);
            _mockOtpService.Verify(s => s.CreateOtpEntity(
                It.Is<string>(e => e == email),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendOtpEmailAsync_StaffNotFound_ThrowsNotFoundException()
        {
            var email = "nonexistent@example.com";
            var request = new SendOtpEmailRequest { Email = email };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync((Staff)null);

            var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
                _emailService.SendOtpEmailAsync(request));

            Assert.Equal($"Staff with email {email} not found.", exception.Message);
        }

        [Fact]
        public async Task SendOtpEmailAsync_ExceptionDuringProcessing_ThrowsException()
        {
            var email = "test@example.com";
            var request = new SendOtpEmailRequest { Email = email };
            var staff = new Staff { Id = Guid.NewGuid(), Email = email, Name = "Test User" };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync(staff);

            _mockOtpService.Setup(s => s.CreateOtpEntity(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _emailService.SendOtpEmailAsync(request));

            Assert.Equal("Database error", exception.Message);
        }

        private class MockedEmailService : EmailService
        {
            public MockedEmailService(
                IUnitOfWork<ClaimRequestDbContext> unitOfWork,
                IConfiguration configuration,
                IClaimService claimService,
                ILogger<EmailService> logger,
                IProjectService projectService,
                IStaffService staffService,
                IOtpService otpService,
                OtpUtil otpUtil)
                : base(unitOfWork, configuration, claimService, logger, projectService, staffService, otpService, otpUtil)
            {
            }

            public override Task SendEmailAsync(string recipientEmail, string subject, string body)
            {
                return Task.CompletedTask;
            }

            protected Task<string> ReadTemplateFileAsync(string templatePath)
            {
                return Task.FromResult("<html><body>OTP: {OtpCode}, Expires in {ExpiryTime} minutes</body></html>");
            }
        }
    }
}
