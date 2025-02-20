using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Implements;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;


public class PaidClaimTests : IDisposable
{
    private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
    private readonly Mock<ILogger<Claim>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IDbContextTransaction> _mockTransaction;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IGenericRepository<Claim>> _mockClaimRepository;
    private readonly ClaimRequestDbContext _realDbContext;
    private readonly ClaimService _claimService;

    public PaidClaimTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
        _mockLogger = new Mock<ILogger<Claim>>();
        _mockMapper = new Mock<IMapper>();
        _mockTransaction = new Mock<IDbContextTransaction>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockClaimRepository = new Mock<IGenericRepository<Claim>>();

        var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        _realDbContext = new ClaimRequestDbContext(options);
        _realDbContext.Database.EnsureDeleted();
        _realDbContext.Database.EnsureCreated();

        _mockUnitOfWork.Setup(uow => uow.GetRepository<Claim>())
            .Returns(new GenericRepository<Claim>(_realDbContext));
        _mockUnitOfWork.Setup(uow => uow.Context).Returns(_realDbContext);
        _mockUnitOfWork.Setup(uow => uow.BeginTransactionAsync())
            .ReturnsAsync(_mockTransaction.Object);
        _mockUnitOfWork.Setup(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(uow => uow.RollbackTransactionAsync(It.IsAny<IDbContextTransaction>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(uow => uow.CommitAsync()).ReturnsAsync(1);
        _mockUnitOfWork.Setup(uow => uow.GetRepository<Claim>()).Returns(_mockClaimRepository.Object);

        _claimService = new ClaimService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object
        );
    }

    [Fact]
    
    public async Task PaidClaim_Should_Update_Claim_Status_To_Paid()
    {
        var claimId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var claim = new Claim
        {
            Id = claimId,
            Status = ClaimStatus.Approved,
            Name = "Test Claim",
            Remark = "Approved"
        };

        var paidRequest = new PaidClaimRequest
        {
            ApproverId = approverId,
            PaymentReference = "PAY-12345"
        };

        var paidResponse = new PaidClaimResponse
        {
            Id = claimId,
            ApproverId = approverId,
            Status = ClaimStatus.Paid,
            PaymentReference = "PAY-12345",
            UpdateAt = DateTime.UtcNow
        };

        await _realDbContext.Claims.AddAsync(claim);
        await _realDbContext.SaveChangesAsync();

        _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
            It.IsAny<Expression<Func<Claim, bool>>>(), null, null))
        .ReturnsAsync((Expression<Func<Claim, bool>> predicate, object _, object __) =>
            _realDbContext.Claims.FirstOrDefault(predicate.Compile()));

        _mockMapper.Setup(m => m.Map<PaidClaimResponse>(It.IsAny<Claim>()))
            .Returns(paidResponse);

        var result = await _claimService.PaidClaimResponse(claimId, paidRequest);

        // Fix: Ensure result is cast correctly
        Assert.NotNull(result);
        Assert.IsType<PaidClaimResponse>(result);

        var response = (PaidClaimResponse)result;

        // Fix: Ensure PaymentReference is correctly mapped
        Assert.Equal(ClaimStatus.Paid, response.Status);
        Assert.Equal(paidRequest.PaymentReference, response.PaymentReference);

        _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.RollbackTransactionAsync(It.IsAny<IDbContextTransaction>()), Times.Never);

        _mockLogger.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }


    public void Dispose()
    {
        _realDbContext.Database.EnsureDeleted();
        _mockTransaction.Reset();
        _mockClaimRepository.Reset();
        _mockMapper.Reset();
        _mockHttpContextAccessor.Reset();
        _mockUnitOfWork.Reset();
        GC.SuppressFinalize(this);
    }
}

