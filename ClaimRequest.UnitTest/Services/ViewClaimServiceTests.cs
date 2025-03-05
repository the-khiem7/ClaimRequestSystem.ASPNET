//using System.Collections.Generic;
//using System.Linq.Expressions;
//using System.Linq;
//using System.Threading.Tasks;
//using AutoMapper;
//using ClaimRequest.BLL.Services.Implements;
//using ClaimRequest.DAL.Data.Entities;
//using ClaimRequest.DAL.Data.Exceptions;
//using ClaimRequest.DAL.Data.Responses.Claim;
//using ClaimRequest.DAL.Repositories.Interfaces;
//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore.Query;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Xunit;

//public class ClaimServiceTests
//{
//    private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _unitOfWorkMock;
//    private readonly Mock<ILogger<Claim>> _loggerMock;
//    private readonly Mock<IMapper> _mapperMock;
//    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
//    private readonly ClaimService _claimService;

//    public ClaimServiceTests()
//    {
//        _unitOfWorkMock = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
//        _loggerMock = new Mock<ILogger<Claim>>();
//        _mapperMock = new Mock<IMapper>();
//        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
//        _claimService = new ClaimService(_unitOfWorkMock.Object, _loggerMock.Object, _mapperMock.Object, _httpContextAccessorMock.Object);
//    }

//    [Fact]
//    public async Task GetClaims_ValidStatus_ReturnsClaims()
//    {
//        // Arrange
//        var claims = new List<Claim>
//        {
//            new Claim { Id = Guid.NewGuid(), Status = ClaimStatus.Draft },
//            new Claim { Id = Guid.NewGuid(), Status = ClaimStatus.Draft }
//        };

//        var claimResponses = claims.Select(c => new ViewClaimResponse { ProjectName = "Test Project" }).ToList();

//        _unitOfWorkMock.Setup(u => u.GetRepository<Claim>().GetListAsync(
//            It.IsAny<Expression<Func<Claim, bool>>>(),
//            It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
//            It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
//        )).ReturnsAsync(claims);

//        _mapperMock.Setup(m => m.Map<IEnumerable<ViewClaimResponse>>(It.IsAny<IEnumerable<Claim>>()))
//            .Returns(claimResponses);

//        // Act
//        var result = await _claimService.GetClaims(1, 20, ClaimStatus.Draft);

//        // Assert
//        Assert.NotNull(result);
//        Assert.Equal(2, result.Count());

//        // Additional assertions to verify the contents of the result
//        var resultList = result.ToList();
//        Assert.Equal("Test Project", resultList[0].ProjectName);
//        Assert.Equal("Test Project", resultList[1].ProjectName);
//    }

//        [Fact]
//    public async Task GetClaims_InvalidStatus_ThrowsBadRequestException()
//    {
//        // Act & Assert
//        await Assert.ThrowsAsync<BadRequestException>(() => _claimService.GetClaims((ClaimStatus)999));
//    }
//}