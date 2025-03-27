//using System;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Threading.Tasks;
//using AutoMapper;
//using ClaimRequest.BLL.Services.Implements;
//using ClaimRequest.DAL.Data.Entities;
//using ClaimRequest.DAL.Data.Requests.Project;
//using ClaimRequest.DAL.Repositories.Interfaces;
//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Query; 
//using Microsoft.Extensions.Logging;
//using Moq;
//using Xunit;

//namespace ClaimRequest.UnitTest.Services
//{
//    public class UpdateProjectServiceTests : IDisposable
//    {
//        private readonly Mock<ILogger<Project>> _mockLogger;
//        private readonly Mock<IMapper> _mockMapper;
//        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
//        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
//        private readonly ClaimRequestDbContext _dbContext;
//        private readonly ProjectService _projectService;

//        public UpdateProjectServiceTests()
//        {
//            _mockLogger = new Mock<ILogger<Project>>();
//            _mockMapper = new Mock<IMapper>();
//            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
//            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();

//            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
//                .UseInMemoryDatabase("UpdateProjectTestDb")
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
//        public async Task UpdateProject_ShouldThrow_WhenProjectDoesNotExist()
//        {
//            var id = Guid.NewGuid();
//            var request = new UpdateProjectRequest();

//            var repoMock = new Mock<IGenericRepository<Project>>();

//            // ✅ Fix: place include in the 3rd parameter, not the 2nd
//            repoMock.Setup(r => r.SingleOrDefaultAsync(
//                It.IsAny<Expression<Func<Project, bool>>>(),
//                null,
//                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
//                .ReturnsAsync((Project)null!);

//            _mockUnitOfWork.Setup(u => u.GetRepository<Project>()).Returns(repoMock.Object);
//            _mockUnitOfWork.Setup(u => u.Context.Database.CreateExecutionStrategy())
//                .Returns(_dbContext.Database.CreateExecutionStrategy());

//            await Assert.ThrowsAsync<Exception>(() => _projectService.UpdateProject(id, request));
//        }

//        public void Dispose()
//        {
//            _dbContext.Database.EnsureDeleted();
//            _mockHttpContextAccessor.Reset();
//            GC.SuppressFinalize(this);
//        }
//    }
//}
