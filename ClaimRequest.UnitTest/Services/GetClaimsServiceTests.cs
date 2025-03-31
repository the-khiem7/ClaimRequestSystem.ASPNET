using System.Linq.Expressions;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using ClaimEntity = ClaimRequest.DAL.Data.Entities.Claim;

namespace ClaimRequest.UnitTest.Services
{
    public class GetClaimsServiceTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<ClaimEntity>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<ClaimEntity>> _mockClaimRepository;
        private readonly ClaimService _claimService;

        public GetClaimsServiceTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<ClaimEntity>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<ClaimEntity>>();

            _mockUnitOfWork
                .Setup(uow => uow.GetRepository<ClaimEntity>())
                .Returns(_mockClaimRepository.Object);

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
            var claims = new List<ClaimEntity>
        {
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Pending,
                UpdateAt = DateTime.UtcNow,
            },
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Approved,
                UpdateAt = DateTime.UtcNow,
            },
        };

            var pagedResponse = new PagingResponse<ViewClaimResponse>
            {
                Items = claims
                    .Select(c => new ViewClaimResponse { Id = c.Id, Status = c.Status })
                    .ToList(),
            };

            var staffId = Guid.NewGuid().ToString();
            var role = SystemRole.Admin.ToString();

            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", staffId);
                        if (claimType == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            return new System.Security.Claims.Claim(claimType, role);

                        return null;
                    }
                );

            _mockMapper
                .Setup(m => m.Map<ViewClaimResponse>(It.IsAny<ClaimEntity>()))
                .Returns((ClaimEntity c) => new ViewClaimResponse { Id = c.Id, Status = c.Status });

            _mockClaimRepository
                .Setup(r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    )
                )
                .ReturnsAsync(pagedResponse);

            var result = await _claimService.GetClaims();

            Assert.NotNull(result);
            Assert.Equal(claims.Count, result.Items.Count());

            _mockClaimRepository.Verify(
                r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ShouldReturnClaimWithDraftStatus()
        {
            var claims = new List<ClaimEntity>
        {
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Draft,
                UpdateAt = DateTime.UtcNow,
            },
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Approved,
                UpdateAt = DateTime.UtcNow,
            },
        };
            var pagedResponse = new PagingResponse<ViewClaimResponse>
            {
                Items = claims
                    .Select(c => new ViewClaimResponse { Id = c.Id, Status = c.Status })
                    .ToList(),
            };
            var staffId = Guid.NewGuid().ToString();
            var role = SystemRole.Admin.ToString();
            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", staffId);
                        if (claimType == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            return new System.Security.Claims.Claim(claimType, role);
                        return null;
                    }
                );
            _mockMapper
                .Setup(m => m.Map<ViewClaimResponse>(It.IsAny<ClaimEntity>()))
                .Returns((ClaimEntity c) => new ViewClaimResponse { Id = c.Id, Status = c.Status });
            _mockClaimRepository
                .Setup(r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    )
                )
                .ReturnsAsync(pagedResponse);
            var result = await _claimService.GetClaims();
            Assert.NotNull(result);
            Assert.Equal(claims.Count, result.Items.Count());
            _mockClaimRepository.Verify(
                r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ShouldReturnClaimWithSortByIdDescending()
        {
            var claims = new List<ClaimEntity>
        {
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Draft,
                UpdateAt = DateTime.UtcNow,
            },
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Approved,
                UpdateAt = DateTime.UtcNow,
            },
        };
            var pagedResponse = new PagingResponse<ViewClaimResponse>
            {
                Items = claims
                    .Select(c => new ViewClaimResponse { Id = c.Id, Status = c.Status })
                    .ToList(),
            };
            var staffId = Guid.NewGuid().ToString();
            var role = SystemRole.Admin.ToString();
            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", staffId);
                        if (claimType == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            return new System.Security.Claims.Claim(claimType, role);
                        return null;
                    }
                );
            _mockMapper
                .Setup(m => m.Map<ViewClaimResponse>(It.IsAny<ClaimEntity>()))
                .Returns((ClaimEntity c) => new ViewClaimResponse { Id = c.Id, Status = c.Status });
            _mockClaimRepository
                .Setup(r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    )
                )
                .ReturnsAsync(pagedResponse);
            var result = await _claimService.GetClaims(1, 20, null, "ClaimerMode", null, "id", true);
            Assert.NotNull(result);
            Assert.Equal(claims.Count, result.Items.Count());
            _mockClaimRepository.Verify(
                r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ShouldReturnClaimWithSearchFilter()
        {
            var claims = new List<ClaimEntity>
        {
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Draft,
                UpdateAt = DateTime.UtcNow,
            },
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Approved,
                UpdateAt = DateTime.UtcNow,
            },
        };
            var pagedResponse = new PagingResponse<ViewClaimResponse>
            {
                Items = claims
                    .Select(c => new ViewClaimResponse { Id = c.Id, Status = c.Status })
                    .ToList(),
            };
            var staffId = Guid.NewGuid().ToString();
            var role = SystemRole.Admin.ToString();
            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", staffId);
                        if (claimType == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            return new System.Security.Claims.Claim(claimType, role);
                        return null;
                    }
                );
            _mockMapper
                .Setup(m => m.Map<ViewClaimResponse>(It.IsAny<ClaimEntity>()))
                .Returns((ClaimEntity c) => new ViewClaimResponse { Id = c.Id, Status = c.Status });
            _mockClaimRepository
                .Setup(r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    )
                )
                .ReturnsAsync(pagedResponse);
            var result = await _claimService.GetClaims(
                1,
                20,
                null,
                "ClaimerMode",
                "search",
                "id",
                true
            );
            Assert.NotNull(result);
            Assert.Equal(claims.Count, result.Items.Count());
            _mockClaimRepository.Verify(
                r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ShouldReturnWithDifferentPageNumber()
        {
            var claims = new List<ClaimEntity>
        {
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Draft,
                UpdateAt = DateTime.UtcNow,
            },
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Approved,
                UpdateAt = DateTime.UtcNow,
            },
        };

            int pageNumber = 2;
            int pageSize = 1;
            var expectedClaim = claims
                .OrderBy(c => c.UpdateAt)
                .Skip((pageNumber - 1) * pageSize)
                .First();

            var staffId = Guid.NewGuid().ToString();
            var role = SystemRole.Admin.ToString();
            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", staffId);
                        if (claimType == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            return new System.Security.Claims.Claim(claimType, role);
                        return null;
                    }
                );

            _mockMapper
                .Setup(m => m.Map<ViewClaimResponse>(It.IsAny<ClaimEntity>()))
                .Returns((ClaimEntity c) => new ViewClaimResponse { Id = c.Id, Status = c.Status });

            var pagedResponse = new PagingResponse<ViewClaimResponse>
            {
                Items = new List<ViewClaimResponse>
            {
                new ViewClaimResponse { Id = expectedClaim.Id, Status = expectedClaim.Status },
            },
            };

            _mockClaimRepository
                .Setup(r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.Is<int>(p => p == pageNumber),
                        It.Is<int>(s => s == pageSize)
                    )
                )
                .ReturnsAsync(pagedResponse);

            var result = await _claimService.GetClaims(
                pageNumber,
                pageSize,
                null,
                "ClaimerMode",
                "search",
                "id",
                true
            );

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(expectedClaim.Id, result.Items.First().Id);

            _mockClaimRepository.Verify(
                r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.Is<int>(p => p == pageNumber),
                        It.Is<int>(s => s == pageSize)
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ShouldReturnWithDateRangeUpdateAt()
        {
            var claims = new List<ClaimEntity>
        {
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Draft,
                UpdateAt = DateTime.UtcNow,
            },
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Approved,
                UpdateAt = DateTime.UtcNow,
            },
        };
            var fromDate = DateTime.UtcNow.AddDays(-1);
            var toDate = DateTime.UtcNow.AddDays(1);
            var pagedResponse = new PagingResponse<ViewClaimResponse>
            {
                Items = claims
                    .Select(c => new ViewClaimResponse { Id = c.Id, Status = c.Status })
                    .ToList(),
            };

            var staffId = Guid.NewGuid().ToString();
            var role = SystemRole.Admin.ToString();
            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", staffId);
                        if (claimType == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            return new System.Security.Claims.Claim(claimType, role);
                        return null;
                    }
                );
            _mockMapper
                .Setup(m => m.Map<ViewClaimResponse>(It.IsAny<ClaimEntity>()))
                .Returns((ClaimEntity c) => new ViewClaimResponse { Id = c.Id, Status = c.Status });
            _mockClaimRepository
                .Setup(r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    )
                )
                .ReturnsAsync(pagedResponse);

            var result = await _claimService.GetClaims(
                1,
                20,
                null,
                "ClaimerMode",
                "search",
                "id",
                true,
                fromDate,
                toDate
            );

            Assert.NotNull(result);
            Assert.Equal(claims.Count, result.Items.Count());

            _mockClaimRepository.Verify(
                r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task UnauthorizedViewModeError()
        {
            var staffId = Guid.NewGuid().ToString();
            var role = SystemRole.Admin.ToString();
            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", staffId);
                        if (claimType == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            return new System.Security.Claims.Claim(claimType, role);
                        return null;
                    }
                );
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () =>
                    await _claimService.GetClaims(1, 20, null, "ApproverMode", "search", "id", true)
            );
        }


        [Fact]
        public async Task ShouldReturnClaimWithViewModeApprover()
        {
            var claims = new List<ClaimEntity>
        {
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Draft,
                UpdateAt = DateTime.UtcNow,
            },
            new ClaimEntity
            {
                Id = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid(),
                Status = ClaimStatus.Approved,
                UpdateAt = DateTime.UtcNow,
            },
        };
            var pagedResponse = new PagingResponse<ViewClaimResponse>
            {
                Items = claims
                    .Select(c => new ViewClaimResponse { Id = c.Id, Status = c.Status })
                    .ToList(),
            };
            var staffId = Guid.NewGuid().ToString();
            var role = SystemRole.Admin.ToString();
            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", staffId);
                        if (claimType == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            return new System.Security.Claims.Claim(claimType, role);
                        return null;
                    }
                );
            _mockMapper
                .Setup(m => m.Map<ViewClaimResponse>(It.IsAny<ClaimEntity>()))
                .Returns((ClaimEntity c) => new ViewClaimResponse { Id = c.Id, Status = c.Status });
            _mockClaimRepository
                .Setup(r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                        >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                    )
                )
                .ReturnsAsync(pagedResponse);
            var result = await _claimService.GetClaims(1, 20, null, "ClaimerMode", "search", "id", true);
            Assert.NotNull(result);
            Assert.Equal(claims.Count, result.Items.Count());
            _mockClaimRepository.Verify(
                r =>
                    r.GetPagingListAsync(
                        It.IsAny<Expression<Func<ClaimEntity, ViewClaimResponse>>>(),
                        It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                        It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                        It.IsAny<
                            Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>
                            >(),
                        It.IsAny<int>(),
                        It.IsAny<int>()
                        ),
                Times.Once
                );
        }


    }
}