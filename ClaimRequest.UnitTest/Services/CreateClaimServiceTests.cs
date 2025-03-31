using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class CreateClaimServiceTests : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<Claim>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<Claim>> _mockClaimRepository;
        private readonly ClaimService _claimService;
        private readonly ClaimRequestDbContext _realDbContext;

        public CreateClaimServiceTests()
        {
            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            _realDbContext = new ClaimRequestDbContext(options);
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<Claim>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<Claim>>();

            _mockUnitOfWork.Setup(u => u.Context).Returns(_realDbContext);
            _mockUnitOfWork.Setup(u => u.GetRepository<Claim>()).Returns(_mockClaimRepository.Object);

            _claimService = new ClaimService(_mockUnitOfWork.Object, _mockLogger.Object, _mockMapper.Object, _mockHttpContextAccessor.Object);
        }

        public void Dispose() => _realDbContext.Dispose();

        [Fact]
        public async Task CreateClaim_ShouldReturn_CreateClaimResponse_WhenSuccessful()
        {
            var createClaimRequest = new CreateClaimRequest
            {
                ClaimType = ClaimType.OvertimeCompensation,
                Name = "Test Claim",
                Amount = 100,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                TotalWorkingHours = 8,
                CreateAt = DateTime.UtcNow,
                ProjectId = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid()
            };

            var claim = new Claim
            {
                Id = Guid.NewGuid(),
                ClaimType = createClaimRequest.ClaimType,
                Name = createClaimRequest.Name,
                Amount = createClaimRequest.Amount,
                StartDate = createClaimRequest.StartDate,
                EndDate = createClaimRequest.EndDate,
                TotalWorkingHours = createClaimRequest.TotalWorkingHours,
                CreateAt = createClaimRequest.CreateAt,
                Status = ClaimStatus.Draft,
                ProjectId = createClaimRequest.ProjectId,
                ClaimerId = createClaimRequest.ClaimerId
            };

            var expectedResponse = new CreateClaimResponse
            {
                ClaimType = claim.ClaimType,
                Name = claim.Name,
                CreateAt = claim.CreateAt
            };

            _mockMapper.Setup(m => m.Map<Claim>(createClaimRequest)).Returns(claim);
            _mockMapper.Setup(m => m.Map<CreateClaimResponse>(claim)).Returns(expectedResponse);
            _mockClaimRepository.Setup(repo => repo.InsertAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<CreateClaimResponse>>>()))
                .Returns<Func<Task<CreateClaimResponse>>>(async func => await func());

            var result = await _claimService.CreateClaim(createClaimRequest);

            Assert.NotNull(result);
            Assert.Equal(expectedResponse.ClaimType, result.ClaimType);
            Assert.Equal(expectedResponse.Name, result.Name);
            Assert.Equal(expectedResponse.CreateAt, result.CreateAt);
            _mockClaimRepository.Verify(repo => repo.InsertAsync(It.IsAny<Claim>()), Times.Once);
        }

        [Fact]
        public async Task CreateClaim_ShouldThrowException_WhenRepositoryFails()
        {
            var createClaimRequest = new CreateClaimRequest
            {
                ClaimType = ClaimType.OvertimeCompensation,
                Name = "Test Claim",
                Amount = 100,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                TotalWorkingHours = 8,
                CreateAt = DateTime.UtcNow,
                ProjectId = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid()
            };

            var claim = new Claim
            {
                Id = Guid.NewGuid(),
                ClaimType = createClaimRequest.ClaimType,
                Name = createClaimRequest.Name
            };

            _mockMapper.Setup(m => m.Map<Claim>(createClaimRequest)).Returns(claim);
            _mockClaimRepository.Setup(repo => repo.InsertAsync(It.IsAny<Claim>())).ThrowsAsync(new Exception("Database error"));

            _mockUnitOfWork
                .Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<CreateClaimResponse>>>()))
                .Returns<Func<Task<CreateClaimResponse>>>(async func =>
                {
                    return await func();
                });

            var exception = await Assert.ThrowsAsync<Exception>(() => _claimService.CreateClaim(createClaimRequest));
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public async Task CreateClaim_ShouldLogError_WhenExceptionOccurs()
        {
            var createClaimRequest = new CreateClaimRequest
            {
                ClaimType = ClaimType.OvertimeCompensation,
                Name = "Test Claim",
                Amount = 100,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                TotalWorkingHours = 8,
                CreateAt = DateTime.UtcNow,
                ProjectId = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid()
            };

            var expectedError = new Exception("Database error");

            _mockMapper.Setup(m => m.Map<Claim>(createClaimRequest)).Throws(expectedError);

            _mockUnitOfWork
                .Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<CreateClaimResponse>>>()))
                .ThrowsAsync(expectedError);

            var exception = await Assert.ThrowsAsync<Exception>(() => _claimService.CreateClaim(createClaimRequest));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error creating claim")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
