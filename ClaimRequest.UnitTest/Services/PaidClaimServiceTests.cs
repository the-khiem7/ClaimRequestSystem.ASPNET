using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Repositories.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Claim;

public class PaidClaimServiceTests
{
    private readonly Mock<IClaimRepository> _mockClaimRepository;
    private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly ILogger<Claim> _mockLogger;
    private readonly ClaimService _claimService;

    public PaidClaimServiceTests()
    {
        _mockClaimRepository = new Mock<IClaimRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
        _mockMapper = new Mock<IMapper>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = NullLogger<Claim>.Instance; // Dùng NullLogger nếu không cần kiểm tra log

        _claimService = new ClaimService(
            _mockUnitOfWork.Object,
            _mockLogger,
            _mockMapper.Object,
            _mockHttpContextAccessor.Object,
            _mockClaimRepository.Object
        );
    }

    [Fact]
    public async Task PaidClaim_ShouldThrowNotFoundException_WhenClaimNotFound()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var request = new PaidClaimRequest
        {
            PaidDate = DateTime.UtcNow,
            PaidAmount = 1000
        };

        _mockClaimRepository.Setup(repo => repo.GetClaimByIdAsync(claimId))
            .ReturnsAsync((Claim)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _claimService.PaidClaim(claimId, request));
    }

    [Fact]
    public async Task PaidClaim_ShouldThrowBadRequestException_WhenClaimAlreadyPaid()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var request = new PaidClaimRequest
        {
            PaidDate = DateTime.UtcNow,
            PaidAmount = 1000
        };

        var claim = new Claim
        {
            Id = claimId,
            Status = ClaimStatus.Paid
        };

        _mockClaimRepository.Setup(repo => repo.GetClaimByIdAsync(claimId))
            .ReturnsAsync(claim);
        _mockClaimRepository.Setup(repo => repo.IsClaimPaidAsync(claimId))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _claimService.PaidClaim(claimId, request));
    }

    [Fact]
    public async Task PaidClaim_ShouldUpdateClaimSuccessfully_WhenClaimIsValid()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var request = new PaidClaimRequest
        {
            PaidDate = DateTime.UtcNow,
            PaidAmount = 1000
        };

        var claim = new Claim
        {
            Id = claimId,
            Status = ClaimStatus.Pending // Giả sử trạng thái ban đầu là Pending
        };

        _mockClaimRepository.Setup(repo => repo.GetClaimByIdAsync(claimId))
            .ReturnsAsync(claim);
        _mockClaimRepository.Setup(repo => repo.IsClaimPaidAsync(claimId))
            .ReturnsAsync(false);
        _mockClaimRepository.Setup(repo => repo.MarkClaimAsPaidAsync(claimId, request.PaidDate, request.PaidAmount))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _claimService.PaidClaim(claimId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(claimId, result.ClaimId);
        Assert.Equal(request.PaidDate, result.PaidDate);
        Assert.Equal(request.PaidAmount, result.PaidAmount);
        Assert.Equal("Paid", result.Status);
    }
}
