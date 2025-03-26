using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
using Xunit;

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
        // Remove readonly
        private Mock<ILogger<EmailService>> _mockLogger;
        private readonly EmailService _emailService;
        private readonly OtpUtil _otpUtil;

        public EmailServiceTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockClaimService = new Mock<IClaimService>();
            _mockProjectService = new Mock<IProjectService>();
            _mockStaffService = new Mock<IStaffService>();
            _mockOtpService = new Mock<IOtpService>();
            _mockLogger = new Mock<ILogger<EmailService>>();

            // Setup configuration settings for _senderEmail and OtpUtil
            _mockConfiguration.Setup(config => config["EmailSettings:SenderEmail"]).Returns("sender@example.com");
            _mockConfiguration.Setup(config => config["OtpSettings:SecretSalt"]).Returns("some_secret_salt");

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
        public async Task SendOtpEmailAsync_Should_Return_Success_When_Staff_Exists_And_Email_Sent()
        {
            // Arrange
            var request = new SendOtpEmailRequest { Email = "test@example.com" };
            var existingStaff = new Staff { Email = request.Email };

            var mockStaffRepo = new Mock<IGenericRepository<Staff>>();
            mockStaffRepo.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync(existingStaff);

            _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>())
                           .Returns(mockStaffRepo.Object);

            _mockOtpService.Setup(otpService => otpService.CreateOtpEntity(request.Email, It.IsAny<string>()))
                           .Returns(Task.CompletedTask);

            // Partial mock of EmailService
            var emailServicePartialMock = new Mock<EmailService>(
        _mockUnitOfWork.Object,
        _mockConfiguration.Object,
        _mockClaimService.Object,
        _mockLogger.Object,
        _mockProjectService.Object,
        _mockStaffService.Object,
        _mockOtpService.Object,
        _otpUtil)
            {
                CallBase = true
            };

            emailServicePartialMock.Setup(service => service.SendEmailAsync(
                request.Email,
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var response = await emailServicePartialMock.Object.SendOtpEmailAsync(request);

            // Assert
            Assert.True(response.Success);

            // Verify that dependencies were called
            mockStaffRepo.Verify(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null), Times.Once);

            _mockOtpService.Verify(otpService => otpService.CreateOtpEntity(
                request.Email, It.IsAny<string>()), Times.Once);

            emailServicePartialMock.Verify(service => service.SendEmailAsync(
                request.Email, "Your OTP Code", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendOtpEmailAsync_Should_Throw_NotFoundException_When_Staff_Does_Not_Exist()
        {
            // Arrange
            var request = new SendOtpEmailRequest { Email = "nonexistent@example.com" };

            var mockStaffRepo = new Mock<IGenericRepository<Staff>>();
            mockStaffRepo.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                         .ReturnsAsync((Staff)null);

            _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>())
                           .Returns(mockStaffRepo.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendOtpEmailAsync(request));

            mockStaffRepo.Verify(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null), Times.Once);
        }

        [Fact]
        public async Task SendOtpEmailAsync_Should_Rethrow_Exception_When_SendEmailAsync_Fails()
        {
            // Arrange
            var request = new SendOtpEmailRequest { Email = "test@example.com" };
            var existingStaff = new Staff { Email = request.Email };

            var mockStaffRepo = new Mock<IGenericRepository<Staff>>();
            mockStaffRepo.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync(existingStaff);

            _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>())
                           .Returns(mockStaffRepo.Object);

            _mockOtpService.Setup(otpService => otpService.CreateOtpEntity(request.Email, It.IsAny<string>()))
                           .Returns(Task.CompletedTask);

            var exceptionThrown = new Exception("Email sending failed.");

            // Set up the mock logger
            _mockLogger = new Mock<ILogger<EmailService>>();
            _mockLogger.Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));

            // Mocking SendEmailAsync method to throw exception
            var emailServicePartialMock = new Mock<EmailService>(
                _mockUnitOfWork.Object,
                _mockConfiguration.Object,
                _mockClaimService.Object,
                _mockLogger.Object,
                _mockProjectService.Object,
                _mockStaffService.Object,
                _mockOtpService.Object,
                _otpUtil)
            {
                CallBase = true
            };

            emailServicePartialMock.Setup(service => service.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ThrowsAsync(exceptionThrown);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => emailServicePartialMock.Object.SendOtpEmailAsync(request));

            // Assert - compare exception messages
            Assert.Equal(exceptionThrown.Message, ex.Message);
            // Verify that the logger was called with the error
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send OTP email.")),
                exceptionThrown,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
