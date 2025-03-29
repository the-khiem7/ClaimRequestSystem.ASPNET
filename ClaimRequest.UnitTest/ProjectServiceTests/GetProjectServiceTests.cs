//using System;
//using System.Threading.Tasks;
//using AutoMapper;
//using ClaimRequest.BLL.Services.Implements;
//using ClaimRequest.DAL.Data.Entities;
//using ClaimRequest.DAL.Data.Responses.Project;
//using ClaimRequest.DAL.Repositories.Interfaces;
//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Xunit;
//using System.Linq.Expressions;
//using Microsoft.EntityFrameworkCore.Query;

//namespace ClaimRequest.UnitTest.Services
//{
//    public class GetProjectServiceTests : IDisposable
//    {
//        private readonly Mock<ILogger<Project>> _mockLogger;
//        private readonly Mock<IMapper> _mockMapper;
//        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
//        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
//        private readonly ClaimRequestDbContext _dbContext;
//        private readonly ProjectService _projectService;

//        public GetProjectServiceTests()
//        {
//            _mockLogger = new Mock<ILogger<Project>>();
//            _mockMapper = new Mock<IMapper>();
//            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
//            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();

//            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
//                .UseInMemoryDatabase("GetProjectTestDb")
//                .Options;

//            _dbContext = new ClaimRequestDbContext(options);
//            _dbContext.Database.EnsureDeleted();
//            _dbContext.Database.EnsureCreated();

//            _mockUnitOfWork.Setup(u => u.Context).Returns(_dbContext);

//            _projectService = new ProjectService(
//                _mockUnitOfWork.Object,
//                _mockLogger.Object,
//                _mockMapper.Object,
//                _mockHttpContextAccessor.Object
//            );
//        }

//        [Fact]
//        public async Task GetProjectById_ShouldReturnResponse_WhenProjectExists()
//        {
//            var id = Guid.NewGuid();
//            var project = new Project { Id = id, Name = "Sample Project", IsActive = true };

//            var repoMock = new Mock<IGenericRepository<Project>>();
//            repoMock.Setup(r => r.SingleOrDefaultAsync(
//                It.IsAny<Expression<Func<Project, bool>>>(),
//                null, // orderBy
//                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
//                .ReturnsAsync(project); // ✅ Now valid

//            _mockUnitOfWork.Setup(u => u.GetRepository<Project>())
//                .Returns(repoMock.Object);

//            _mockMapper.Setup(m => m.Map<CreateProjectResponse>(It.IsAny<Project>()))
//                .Returns(new CreateProjectResponse { Id = id, Name = "Sample Project" });

//            var result = await _projectService.GetProjectById(id);

//            Assert.NotNull(result);
//            Assert.Equal(id, result.Id);
//        }

//    }
//}
