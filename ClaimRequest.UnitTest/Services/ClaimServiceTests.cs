using System.Linq.Expressions;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Mappers;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
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

    public class ClaimServiceTests
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<Claim>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly IMapper _mapper;
        private readonly ClaimService _claimService;
        private readonly Mock<IGenericRepository<Claim>> _mockClaimRepository;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly TestClaimRequestDbContext _dbContext;
        private readonly Mock<DatabaseFacade> _mockDatabase;

        public ClaimServiceTests()
        {
            // Initialize mocks
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<Claim>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<Claim>>();
            _mockTransaction = new Mock<IDbContextTransaction>();
            _dbContext = new TestClaimRequestDbContext();
            _mockDatabase = new Mock<DatabaseFacade>(_dbContext);

            // Setup mapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ClaimMapper>();
            });
            _mapper = mapperConfig.CreateMapper();

            // Setup database
            _mockDatabase.Setup(d => d.CreateExecutionStrategy())
                .Returns(new MockExecutionStrategy());
            _mockDatabase.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockTransaction.Object);

            // Setup unit of work
            _mockUnitOfWork.Setup(uow => uow.Context)
                .Returns(_dbContext);

            // Setup transaction
            _mockUnitOfWork.Setup(uow => uow.BeginTransactionAsync())
                .ReturnsAsync(_mockTransaction.Object);

            _mockUnitOfWork.Setup(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()))
                .Returns(Task.CompletedTask);

            // Setup repository
            _mockUnitOfWork.Setup(uow => uow.GetRepository<Claim>())
                .Returns(_mockClaimRepository.Object);

            // Initialize service
            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mapper,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task CreateClaim_ShouldWork()
        {
            // Arrange
            var createClaimRequest = new CreateClaimRequest
            {
                ClaimType = ClaimType.HardwareRequest,
                Name = "Test Hardware Request",
                Remark = "Test Remark",
                Amount = 100.00m,
                ProjectId = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid()
            };

            Claim capturedClaim = null;
            _mockClaimRepository.Setup(repo => repo.InsertAsync(It.IsAny<Claim>()))
                .Callback<Claim>(claim => capturedClaim = claim)
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(uow => uow.CommitAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _claimService.CreateClaim(createClaimRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createClaimRequest.Name, result.Name);
            Assert.Equal(createClaimRequest.Amount, result.Amount);
            Assert.Equal(createClaimRequest.ClaimType, result.ClaimType);

            // Verify the claim was created with correct data
            Assert.NotNull(capturedClaim);
            Assert.Equal(createClaimRequest.Name, capturedClaim.Name);
            Assert.Equal(createClaimRequest.Amount, capturedClaim.Amount);
            Assert.Equal(createClaimRequest.ClaimType, capturedClaim.ClaimType);
            Assert.Equal(ClaimStatus.Draft, capturedClaim.Status);

            // Verify method calls
            _mockClaimRepository.Verify(repo => repo.InsertAsync(It.IsAny<Claim>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()), Times.Once);
        }

        [Fact]
        public async Task ApproveClaim_ShouldWork()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var approveClaimRequest = new ApproveClaimRequest();

            var pendingClaim = new Claim
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ClaimApprovers = new List<ClaimApprover>()
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<Claim, bool>>>(),
                    It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                    It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()))
                .ReturnsAsync(pendingClaim);

            _mockUnitOfWork.SetupGet(uow => uow.GetRepository<Claim>())
                .Returns(_mockClaimRepository.Object);

            var claimApproverRepo = new Mock<IGenericRepository<ClaimApprover>>();
            _mockUnitOfWork.SetupGet(uow => uow.GetRepository<ClaimApprover>())
                .Returns(claimApproverRepo.Object); 

            _mockUnitOfWork.Setup(uow => uow.BeginTransactionAsync())
                .ReturnsAsync(_mockTransaction.Object);

            _mockUnitOfWork.Setup(uow => uow.CommitAsync())
                .ReturnsAsync(1);

            var result = await _claimService.ApproveClaim(claimId, approverId, approveClaimRequest);

            Assert.NotNull(result);
            Assert.Equal(ClaimStatus.Approved, pendingClaim.Status);

            _mockClaimRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Claim>()), Times.Once);
            claimApproverRepo.Verify(repo => repo.InsertAsync(It.IsAny<ClaimApprover>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
            _mockTransaction.Verify(tran => tran.CommitAsync(CancellationToken.None), Times.Once);
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