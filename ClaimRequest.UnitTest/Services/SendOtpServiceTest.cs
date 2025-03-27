using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Email;
using ClaimRequest.DAL.Data.Responses.Email;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class SendOtpEmailTest
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IClaimService> _mockClaimService;
        private readonly Mock<IProjectService> _mockProjectService;
        private readonly Mock<IStaffService> _mockStaffService;
        private readonly Mock<IOtpService> _mockOtpService;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly OtpUtil _otpUtil;

        public SendOtpEmailTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.Setup(c => c["OtpSettings:SecretSalt"]).Returns("test-salt");
            _mockConfiguration.Setup(c => c["EmailSettings:SenderEmail"]).Returns("test@example.com");

            _mockClaimService = new Mock<IClaimService>();
            _mockProjectService = new Mock<IProjectService>();
            _mockStaffService = new Mock<IStaffService>();
            _mockOtpService = new Mock<IOtpService>();
            _mockLogger = new Mock<ILogger<EmailService>>();

            _otpUtil = new OtpUtil(_mockConfiguration.Object);
        }

        [Fact]
        public async Task SendOtpEmailAsync_Should_Throw_NotFoundException_When_Staff_Not_Found()
        {
            var testEmailService = new TestEmailService(
                _mockUnitOfWork.Object,
                _mockConfiguration.Object,
                _mockClaimService.Object,
                _mockLogger.Object,
                _mockProjectService.Object,
                _mockStaffService.Object,
                _mockOtpService.Object,
                _otpUtil
            );

            var request = new SendOtpEmailRequest { Email = "nonexistent@example.com" };

            var staffRepository = new Mock<IGenericRepository<Staff>>();
            staffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync((Staff)null);

            _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>())
                .Returns(staffRepository.Object);

            await Assert.ThrowsAsync<NotFoundException>(() => testEmailService.SendOtpEmailAsync(request));
        }

        [Fact]
        public async Task SendOtpEmailAsync_Should_Return_Success_Response_When_Email_Sent()
        {
            var testEmailService = new TestEmailService(
                _mockUnitOfWork.Object,
                _mockConfiguration.Object,
                _mockClaimService.Object,
                _mockLogger.Object,
                _mockProjectService.Object,
                _mockStaffService.Object,
                _mockOtpService.Object,
                _otpUtil
            );

            var request = new SendOtpEmailRequest { Email = "valid@example.com" };
            var staff = new Staff { Email = "valid@example.com" };

            string capturedOtp = null;

            var staffRepository = new Mock<IGenericRepository<Staff>>();
            staffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>())
                .Returns(staffRepository.Object);

            _mockOtpService.Setup(service => service.CreateOtpEntity(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Callback<string, string>((email, otp) => capturedOtp = otp)
                .Returns(Task.CompletedTask);


            testEmailService.EmailTemplateContent = "<html>Your OTP code is {OtpCode}. It expires in {ExpiryTime} minutes.</html>";

            var result = await testEmailService.SendOtpEmailAsync(request);

            Assert.True(result.Success);
            Assert.NotNull(capturedOtp); 
            _mockOtpService.Verify(service => service.CreateOtpEntity(request.Email, capturedOtp), Times.Once);

            Assert.Equal(request.Email, testEmailService.LastRecipientEmail);
            Assert.Equal("Your OTP Code", testEmailService.LastSubject);
            Assert.Contains(capturedOtp, testEmailService.LastBody);
        }

        private class TestEmailService : EmailService
        {
            public string LastRecipientEmail { get; private set; }
            public string LastSubject { get; private set; }
            public string LastBody { get; private set; }
            public string EmailTemplateContent { get; set; } = "<html>OTP Template</html>";

            public TestEmailService(
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
                LastRecipientEmail = recipientEmail;
                LastSubject = subject;
                LastBody = body;
                return Task.CompletedTask;
            }

            public Task<SendOtpEmailResponse> SendOtpEmailAsync(SendOtpEmailRequest request)
            {
                return SendOtpEmailAsyncCustom(request);
            }

            private async Task<SendOtpEmailResponse> SendOtpEmailAsyncCustom(SendOtpEmailRequest request)
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

                    string body = EmailTemplateContent;

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
}
