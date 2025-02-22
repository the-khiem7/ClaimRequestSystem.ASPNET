using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Implements;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class RejectClaimTests : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<Claim>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<Claim>> _mockClaimRepository;
        private readonly ClaimService _claimService;
        private readonly ClaimRequestDbContext _realDbContext;


        public RejectClaimTests() 
        {
            // Initialize mocks
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<Claim>>();
            _mockMapper = new Mock<IMapper>();
            _mockTransaction = new Mock<IDbContextTransaction>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<Claim>>();

            // Setup real DbContext with in-memory database
            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase("TestDb")  // Use an in-memory database for unit testing
                .Options;

            _realDbContext = new ClaimRequestDbContext(options);
            _realDbContext.Database.EnsureDeleted();  // Ensure database is clean before each test
            _realDbContext.Database.EnsureCreated();  // Create a new database for each test

            _mockUnitOfWork.Setup(uow => uow.GetRepository<Claim>())
                .Returns(new GenericRepository<Claim>(_realDbContext));
            _mockUnitOfWork.Setup(uow => uow.Context).Returns(_realDbContext);


            // Setup unit of work
            _mockUnitOfWork.Setup(uow => uow.BeginTransactionAsync())
                .ReturnsAsync(_mockTransaction.Object);
            _mockUnitOfWork.Setup(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.RollbackTransactionAsync(It.IsAny<IDbContextTransaction>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CommitAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<Claim>()).Returns(_mockClaimRepository.Object);

            // Initialize service
            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }

        // Begin test here
        // Đang fail :[
        [Fact]
        public async Task RejectClaim_Should_Reject_Valid_Claim()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();

            var claim = new Claim
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                Name = "Test Claim",
                Remark = "Initial Remark"
            };

            var rejectRequest = new RejectClaimRequest
            {
                ApproverId = approverId,
                Remark = "Test rejection reason"
            };

            var rejectResponse = new RejectClaimResponse
            {
                Id = claimId,
                ApproverId = approverId,
                Status = ClaimStatus.Rejected,
                Remark = "Test rejection reason",
                UpdateAt = DateTime.UtcNow
            };

            // Add the claim to the in-memory database
            await _realDbContext.Claims.AddAsync(claim);
            await _realDbContext.SaveChangesAsync();

            // Mock repository behavior using SingleOrDefaultAsync
            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(), null, null))
            .ReturnsAsync((Expression<Func<Claim, bool>> predicate, object _, object __) =>
                _realDbContext.Claims.FirstOrDefault(predicate.Compile()));

            _mockMapper.Setup(m => m.Map<RejectClaimResponse>(It.IsAny<Claim>()))
                .Returns(rejectResponse);

            // Act
            var result = await _claimService.RejectClaim(claimId, rejectRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ClaimStatus.Rejected, result.Status);
            Assert.Equal(rejectRequest.Remark, result.Remark);

            // Ensure transactions and commit were handled properly
            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.RollbackTransactionAsync(It.IsAny<IDbContextTransaction>()), Times.Never);

            // Ensure logging was performed
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }



        // End test here


        public void Dispose()
        {
            // Ensure the database is cleaned up and mocks are reset after each test
            _realDbContext.Database.EnsureDeleted();
            _mockTransaction.Reset();
            _mockClaimRepository.Reset();
            _mockMapper.Reset();
            _mockHttpContextAccessor.Reset();
            _mockUnitOfWork.Reset();
            GC.SuppressFinalize(this);
        }



    }
}
