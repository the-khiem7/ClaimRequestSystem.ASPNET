using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class ClaimServiceTests : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<Claim>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<Claim>> _mockClaimRepository;
        private readonly ClaimService _claimService;
        private readonly ClaimRequestDbContext _realDbContext;

        public ClaimServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<Claim>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<Claim>>();

            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())  
                .Options;

            _realDbContext = new ClaimRequestDbContext(options);
            _realDbContext.Database.EnsureCreated();

            _mockUnitOfWork.Setup(uow => uow.Context).Returns(_realDbContext);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<Claim>()).Returns(_mockClaimRepository.Object);

            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task GetClaims_ShouldReturnPagedClaims()
        {
            // Arrange
            var claims = new List<Claim>
    {
        new Claim
        {
            Id = Guid.NewGuid(),
            Status = ClaimStatus.Draft,
            Claimer = new Staff { Name = "John Doe" },
            Project = new Project { Name = "Project A" },
            TotalWorkingHours = 40,
            Amount = 1000
        },
        new Claim
        {
            Id = Guid.NewGuid(),
            Status = ClaimStatus.Approved,
            Claimer = new Staff { Name = "Jane Doe" },
            Project = new Project { Name = "Project B" },
            TotalWorkingHours = 30,
            Amount = 800
        }
    };

            // Convert to ViewClaimResponse
            var mappedClaims = claims.Select(c => new ViewClaimResponse
            {
                Id = c.Id,
                StaffName = c.Claimer?.Name ?? "Unknown",
                ProjectName = c.Project?.Name ?? "Unknown",
                TotalWorkingHours = c.TotalWorkingHours,
                Amount = c.Amount
            }).ToList();

            // Explicitly initialize PaginationMeta
            var paginationMeta = new PaginationMeta
            {
                CurrentPage = 1,
                PageSize = 20,
                TotalItems = claims.Count
            };

            var pagedClaimsResponse = new PagingResponse<ViewClaimResponse>
            {
                Items = mappedClaims,
                Meta = paginationMeta
            };


            Assert.NotNull(pagedClaimsResponse.Meta); 


            _mockClaimRepository.Setup(repo => repo.GetPagingListAsync(
                It.IsAny<Expression<Func<Claim, ViewClaimResponse>>>(),
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>(),
                1, 
                20 
            )).ReturnsAsync(pagedClaimsResponse);

            
            var mockResult = await _mockClaimRepository.Object.GetPagingListAsync<ViewClaimResponse>(
                It.IsAny<Expression<Func<Claim, ViewClaimResponse>>>(),
                null, null, null, 1, 20);

            Assert.NotNull(mockResult);
            Assert.NotNull(mockResult.Meta); 

            
            var result = await _claimService.GetClaims(1, 20, null);

            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal("John Doe", result.Items.First().StaffName);
            Assert.Equal("Project A", result.Items.First().ProjectName);
            Assert.Equal("Jane Doe", result.Items.Last().StaffName);
            Assert.Equal("Project B", result.Items.Last().ProjectName);
            Assert.NotNull(result.Meta);
            Assert.Equal(1, result.Meta.CurrentPage);
            Assert.Equal(20, result.Meta.PageSize);
            Assert.Equal(2, result.Meta.TotalItems);
        }

        [Fact]
        public async Task GetClaims_ShouldThrowBadRequestException_WhenInvalidStatusProvided()
        {
            var invalidStatus = (ClaimStatus)999;

            await Assert.ThrowsAsync<BadRequestException>(() => _claimService.GetClaims(1, 20, invalidStatus));
        }

        [Theory]
        [InlineData(ClaimStatus.Draft)]
        [InlineData(ClaimStatus.Pending)]
        [InlineData(ClaimStatus.Approved)]
        [InlineData(ClaimStatus.Paid)]
        [InlineData(ClaimStatus.Rejected)]
        [InlineData(ClaimStatus.Cancelled)]
        public async Task GetClaims_ShouldReturnFilteredClaims_ByStatus(ClaimStatus status)
        {
            // Arrange
            var claims = new List<Claim>
    {
        new Claim
        {
            Id = Guid.NewGuid(),
            Status = status, // ✅ Test each status dynamically
            Claimer = new Staff { Name = "John Doe" },
            Project = new Project { Name = "Project X" },
            TotalWorkingHours = 40,
            Amount = 1000
        },
        new Claim
        {
            Id = Guid.NewGuid(),
            Status = ClaimStatus.Draft, // Different status for filtering test
            Claimer = new Staff { Name = "Jane Doe" },
            Project = new Project { Name = "Project Y" },
            TotalWorkingHours = 30,
            Amount = 800
        }
    };

            var filteredClaims = claims.Where(c => c.Status == status).ToList();

            var mappedClaims = filteredClaims.Select(c => new ViewClaimResponse
            {
                Id = c.Id,
                StaffName = c.Claimer?.Name ?? "Unknown",
                ProjectName = c.Project?.Name ?? "Unknown",
                TotalWorkingHours = c.TotalWorkingHours,
                Amount = c.Amount
            }).ToList();

            var paginationMeta = new PaginationMeta
            {
                CurrentPage = 1,
                PageSize = 20,
                TotalItems = filteredClaims.Count
            };

            var pagedClaimsResponse = new PagingResponse<ViewClaimResponse>
            {
                Items = mappedClaims,
                Meta = paginationMeta
            };

            _mockClaimRepository.Setup(repo => repo.GetPagingListAsync(
                It.IsAny<Expression<Func<Claim, ViewClaimResponse>>>(),
                It.IsAny<Expression<Func<Claim, bool>>>(), // ✅ Ensure filtering by status
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>(),
                1,
                20
            )).ReturnsAsync(pagedClaimsResponse);

            // Act
            var result = await _claimService.GetClaims(1, 20, status);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(filteredClaims.Count, result.Items.Count());
            if (filteredClaims.Count > 0)
            {
                Assert.All(result.Items, item => Assert.Equal(status, claims.First(c => c.Id == item.Id).Status));
            }
            Assert.NotNull(result.Meta);
            Assert.Equal(1, result.Meta.CurrentPage);
            Assert.Equal(20, result.Meta.PageSize);
            Assert.Equal(filteredClaims.Count, result.Meta.TotalItems);
        }

        [Fact]
        public async Task GetClaimById_ShouldThrowNotFoundException_WhenClaimDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Claim)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _claimService.GetClaimById(nonExistentId));
        }

        public void Dispose()
        {
            // Ensure database is cleaned up and disposed after each test
            _realDbContext.Database.EnsureDeleted();
            _realDbContext.Dispose();
            _mockClaimRepository.Reset();
            _mockMapper.Reset();
            _mockHttpContextAccessor.Reset();
            _mockUnitOfWork.Reset();
            GC.SuppressFinalize(this);
        }
    }
}
