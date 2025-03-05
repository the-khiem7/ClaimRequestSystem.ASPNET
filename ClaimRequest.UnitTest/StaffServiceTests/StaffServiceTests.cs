//using AutoMapper;
//using ClaimRequest.BLL.Services.Implements;
//using ClaimRequest.DAL.Data.Entities;
//using ClaimRequest.DAL.Data.Requests.Staff;
//using ClaimRequest.DAL.Data.Responses.Staff;
//using ClaimRequest.DAL.Repositories.Interfaces;
//using Microsoft.EntityFrameworkCore;
//using Moq;
//using Xunit;
//using ClaimRequest.BLL.Services;
//using Microsoft.Extensions.Configuration;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;

//namespace ClaimRequest.UnitTest.StaffServiceTests
//{
//    public class StaffServiceTests
//    {
//        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
//        private readonly Mock<IMapper> _mockMapper;
//        private readonly Mock<ILogger<StaffService>> _mockLogger;
//        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
//        private readonly Mock<IConfiguration> _mockConfiguration;
//        private readonly Mock<IGenericRepository<Staff>> _mockStaffRepository;
//        private readonly StaffService _staffService;

//        public StaffServiceTests()
//        {
//            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
//            _mockMapper = new Mock<IMapper>();
//            _mockLogger = new Mock<ILogger<StaffService>>();
//            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
//            _mockConfiguration = new Mock<IConfiguration>();
//            _mockStaffRepository = new Mock<IGenericRepository<Staff>>();

//            // Setup UnitOfWork to return mocked repository
//            _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>())
//                           .Returns(_mockStaffRepository.Object);

//            // Initialize StaffService with all required dependencies
//            _staffService = new StaffService(
//                _mockUnitOfWork.Object,
//                _mockLogger.Object,
//                _mockMapper.Object,
//                _mockHttpContextAccessor.Object,
//                _mockConfiguration.Object
//            );
//        }

//        [Fact]
//        public async Task CreateStaffAsync_ShouldThrowException_WhenEmailAlreadyExists()
//        {
//            // Arrange
//            var request = new CreateStaffRequest { Email = "existing@example.com", Password = "123456" };
//            var existingStaff = new Staff { Id = Guid.NewGuid(), Email = "existing@example.com" };

//            _mockStaffRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Func<Staff, bool>>()))
//                                .ReturnsAsync(existingStaff);

//            // Act & Assert
//            var exception = await Assert.ThrowsAsync<Exception>(() => _staffService.CreateStaff(request));
//            Assert.Equal("Email is already in use. Please use a different email.", exception.Message);
//        }

//        [Fact]
//        public async Task CreateStaffAsync_ShouldCreateNewStaff_WhenEmailDoesNotExist()
//        {
//            // Arrange
//            var request = new CreateStaffRequest { Email = "new@example.com", Password = "password123" };
//            var newStaff = new Staff { Id = Guid.NewGuid(), Email = request.Email, Password = "hashed_password" };
//            var expectedResponse = new CreateStaffResponse { Id = newStaff.Id, Email = newStaff.Email };

//            _mockStaffRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Func<Staff, bool>>()))
//                                .ReturnsAsync((Staff)null);

//            _mockMapper.Setup(m => m.Map<Staff>(request)).Returns(newStaff);
//            _mockMapper.Setup(m => m.Map<CreateStaffResponse>(newStaff)).Returns(expectedResponse);

//            _mockStaffRepository.Setup(repo => repo.InsertAsync(It.IsAny<Staff>())).Returns(Task.CompletedTask);
//            _mockUnitOfWork.Setup(uow => uow.CommitAsync()).Returns(Task.CompletedTask);

//            // Act
//            var result = await _staffService.CreateStaff(request);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(request.Email, result.Email);
//            _mockStaffRepository.Verify(repo => repo.InsertAsync(It.IsAny<Staff>()), Times.Once);
//            _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
//        }

//        [Fact]
//        public async Task CreateStaffAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
//        {
//            // Act & Assert
//            await Assert.ThrowsAsync<ArgumentNullException>(() => _staffService.CreateStaff(null));
//        }
//    }
//}
