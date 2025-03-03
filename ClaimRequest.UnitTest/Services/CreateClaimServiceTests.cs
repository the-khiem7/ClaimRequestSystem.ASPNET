using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Mappers;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class TestClaimRequestDbContext : ClaimRequestDbContext
    {
        public TestClaimRequestDbContext()
            : base(new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options)
        {
        }
    }

    public class CreateClaimServiceTests : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<Claim>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly IMapper _mapper;
        private readonly ClaimService _claimService;
        private readonly Mock<IGenericRepository<Claim>> _mockClaimRepository;
        private readonly TestClaimRequestDbContext _dbContext;

        public CreateClaimServiceTests()
        {
            // Initialize mocks
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<Claim>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<Claim>>();
            _dbContext = new TestClaimRequestDbContext();

            // Setup mapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ClaimMapper>();
            });
            _mapper = mapperConfig.CreateMapper();

            // Setup repository
            _mockUnitOfWork.Setup(uow => uow.GetRepository<Claim>())
                .Returns(_mockClaimRepository.Object);

            // Setup unit of work
            _mockUnitOfWork.Setup(uow => uow.CommitAsync())
                .ReturnsAsync(1);

            // Initialize service
            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mapper,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task CreateClaim_ShouldCreateNewClaim()
        {
            // Arrange
            var createClaimRequest = new CreateClaimRequest
            {
                ClaimType = ClaimType.HardwareRequest,
                Name = "Test Hardware Request",
                Remark = "Test Remark",
                Amount = 100.00m,
                ProjectId = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                TotalWorkingHours = 8,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
            };

            Claim capturedClaim = null;
            _mockClaimRepository.Setup(repo => repo.InsertAsync(It.IsAny<Claim>()))
                .Callback<Claim>(claim => capturedClaim = claim)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.CreateClaim(createClaimRequest);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(capturedClaim);

            // Verify the claim was created with correct data
            Assert.Equal(createClaimRequest.Name, capturedClaim.Name);
            Assert.Equal(createClaimRequest.Amount, capturedClaim.Amount);
            Assert.Equal(createClaimRequest.ClaimType, capturedClaim.ClaimType);
            Assert.Equal(createClaimRequest.Remark, capturedClaim.Remark);
            Assert.Equal(createClaimRequest.ProjectId, capturedClaim.ProjectId);
            Assert.Equal(createClaimRequest.ClaimerId, capturedClaim.ClaimerId);
            Assert.Equal(createClaimRequest.TotalWorkingHours, capturedClaim.TotalWorkingHours);
            Assert.Equal(createClaimRequest.StartDate, capturedClaim.StartDate);
            Assert.Equal(createClaimRequest.EndDate, capturedClaim.EndDate);
            Assert.Equal(ClaimStatus.Draft, capturedClaim.Status);

            // Verify repository and unit of work interactions
            _mockClaimRepository.Verify(repo => repo.InsertAsync(It.IsAny<Claim>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }

    // Helper class for mocking execution strategy
    public class MockExecutionStrategy : IExecutionStrategy
    {
        public bool RetriesOnFailure => false;

        public TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
        {
            throw new NotImplementedException();
        }

        public async Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded, CancellationToken cancellationToken = default)
        {
            return await operation(null!, state, cancellationToken);
        }
    }
}