using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using ClaimRequest.DAL.Repositories.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class CloudinaryServiceTests
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<Staff>> _mockStaffRepository;
        private readonly Mock<ILogger<CloudinaryService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<Cloudinary> _mockCloudinary;
        private readonly CloudinaryService _cloudinaryService;

        public CloudinaryServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockStaffRepository = new Mock<IGenericRepository<Staff>>();
            _mockLogger = new Mock<ILogger<CloudinaryService>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockCloudinary = new Mock<Cloudinary>(new Account("cloud_name", "api_key", "api_secret"));

            // Mock configuration
            _mockConfiguration.Setup(c => c["Cloudinary:CloudName"]).Returns("cloud_name");
            _mockConfiguration.Setup(c => c["Cloudinary:ApiKey"]).Returns("api_key");
            _mockConfiguration.Setup(c => c["Cloudinary:ApiSecret"]).Returns("api_secret");

            _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>()).Returns(_mockStaffRepository.Object);

            _cloudinaryService = new CloudinaryService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object,
                _mockConfiguration.Object
            );
        }

        [Fact]
        public async Task UploadImageAsync_ShouldThrowException_WhenFileIsNull()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", Guid.NewGuid().ToString()) }));
            
            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _cloudinaryService.UploadImageAsync(null, user));
            Assert.Equal("No file uploaded.", ex.Message);
        }

        [Fact]
        public async Task UploadImageAsync_ShouldThrowException_WhenFileIsTooLarge()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(6 * 1024 * 1024);
            fileMock.Setup(f => f.FileName).Returns("test.jpg");

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", Guid.NewGuid().ToString()) }));
            
            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _cloudinaryService.UploadImageAsync(fileMock.Object, user));
            Assert.Equal("File size exceeds the 5MB limit.", ex.Message);
        }

        [Fact]
        public async Task UploadImageAsync_ShouldThrowException_WhenFileFormatIsInvalid()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("test.txt");

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", Guid.NewGuid().ToString()) }));

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _cloudinaryService.UploadImageAsync(fileMock.Object, user));
            Assert.Equal("Invalid file format. Allowed formats: .jpg, .jpeg, .png", ex.Message);
        }

        [Fact]
        public async Task UploadImageAsync_ShouldThrowException_WhenStaffNotFound()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("test.jpg");

            var staffId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", staffId.ToString()) }));

            _mockStaffRepository.Setup(repo => repo.GetByIdAsync(staffId)).ReturnsAsync((Staff)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _cloudinaryService.UploadImageAsync(fileMock.Object, user));
            Assert.Equal("Staff not found.", ex.Message);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldThrowException_WhenFileIsTooLarge()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(110 * 1024 * 1024);
            fileMock.Setup(f => f.FileName).Returns("test.pdf");

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", Guid.NewGuid().ToString()) }));

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _cloudinaryService.UploadFileAsync(fileMock.Object, user));
            Assert.Equal("File size exceeds the 100MB limit.", ex.Message);
        }
    }
}
