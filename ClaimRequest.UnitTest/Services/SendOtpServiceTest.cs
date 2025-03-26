//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Threading.Tasks;
//using ClaimRequest.BLL.Services.Implements;
//using ClaimRequest.BLL.Services.Interfaces;
//using ClaimRequest.DAL.Data.Entities;
//using ClaimRequest.DAL.Data.Exceptions;
//using ClaimRequest.DAL.Data.Requests.Email;
//using ClaimRequest.DAL.Repositories.Interfaces;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Xunit;

//namespace ClaimRequest.UnitTest.Services
//{
//    public class SendOtpServiceTest
//    {
//        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _unitOfWorkMock;
//        private readonly Mock<IOtpService> _otpServiceMock;
//        private readonly Mock<ILogger<EmailService>> _loggerMock;
//        private readonly IConfiguration _configuration;
//        private readonly EmailService _emailService;

//        public SendOtpServiceTest()
//        {
//            _unitOfWorkMock = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
//            _otpServiceMock = new Mock<IOtpService>();
//            _loggerMock = new Mock<ILogger<EmailService>>();

//            var inMemorySettings = new Dictionary<string, string>
//                {
//                    { "EmailSettings:Host", "smtp.test.com" },
//                    { "EmailSettings:SmtpPort", "587" },
//                    { "EmailSettings:SenderEmail", "sender@test.com" },
//                    { "EmailSettings:SenderPassword", "password" }
//                };

//            _configuration = new ConfigurationBuilder()
//                .AddInMemoryCollection(inMemorySettings)
//                .Build();

//            // Mocks for other dependencies
//            var claimServiceMock = new Mock<IClaimService>();
//            var projectServiceMock = new Mock<IProjectService>();
//            var staffServiceMock = new Mock<IStaffService>();

//            _emailService = new EmailService(
//                _unitOfWorkMock.Object,
//                _configuration,
//                claimServiceMock.Object,
//                _loggerMock.Object,
//                projectServiceMock.Object,
//                staffServiceMock.Object,
//                _otpServiceMock.Object
//            );
//        }

//        [Fact]
//        public async Task SendOtpEmailAsync_ShouldReturnSuccess_WhenStaffExists()
//        {
//            // Arrange
//            var request = new SendOtpEmailRequest { Email = "testuser@test.com" };
//            var staff = new Staff { Email = "testuser@test.com" };

//            _unitOfWorkMock
//                .Setup(uow => uow.GetRepository<Staff>()
//                .SingleOrDefaultAsync(
//                    It.IsAny<Expression<Func<Staff, bool>>>(),
//                    null, // orderBy
//                    null  // include
//                ))
//                .ReturnsAsync(staff);

//            _otpServiceMock
//                .Setup(otpService => otpService.CreateOtpEntity(It.IsAny<string>(), It.IsAny<string>()))
//                .Returns(Task.CompletedTask);

//            // Act
//            var response = await _emailService.SendOtpEmailAsync(request);

//            // Assert
//            Assert.True(response.Success);
//            try
//            {
//                 response = await _emailService.SendOtpEmailAsync(request);

//                // Assert
//                Assert.True(response.Success);
//            }
//            catch (Exception ex)
//            {
//                // Log the exception message
//                Console.WriteLine($"Test failed with exception: {ex.Message}");
//                throw;
//            }
//            _unitOfWorkMock.Verify(
//                uow => uow.GetRepository<Staff>()
//                .SingleOrDefaultAsync(
//                    It.IsAny<Expression<Func<Staff, bool>>>(),
//                    null, // orderBy
//                    null  // include
//                ),
//                Times.Once
//            );
//            _otpServiceMock.Verify(
//                otpService => otpService.CreateOtpEntity(It.IsAny<string>(), It.IsAny<string>()),
//                Times.Once
//            );
//        }

//        [Fact]
//        public async Task SendOtpEmailAsync_ShouldThrowNotFoundException_WhenStaffDoesNotExist()
//        {
//            // Arrange
//            var request = new SendOtpEmailRequest { Email = "nonexistentuser@test.com" };

//            _unitOfWorkMock
//                .Setup(uow => uow.GetRepository<Staff>()
//                .SingleOrDefaultAsync(
//                    It.IsAny<Expression<Func<Staff, bool>>>(),
//                    null, // orderBy
//                    null  // include
//                ))
//                .ReturnsAsync((Staff)null);

//            // Act & Assert
//            await Assert.ThrowsAsync<NotFoundException>(() => _emailService.SendOtpEmailAsync(request));
//            _unitOfWorkMock.Verify(
//                uow => uow.GetRepository<Staff>()
//                .SingleOrDefaultAsync(
//                    It.IsAny<Expression<Func<Staff, bool>>>(),
//                    null, // orderBy
//                    null  // include
//                ),
//                Times.Once
//            );
//        }
//    }
//}
