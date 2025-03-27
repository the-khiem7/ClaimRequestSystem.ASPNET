//using System;
//using System.Linq.Expressions;
//using System.Threading.Tasks;
//using AutoMapper;
//using ClaimRequest.BLL.Services.Implements;
//using ClaimRequest.DAL.Data.Entities;
//using ClaimRequest.DAL.Repositories.Interfaces;
//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Xunit;

//namespace ClaimRequest.UnitTest.Services
//{
//    public class DeleteProjectServiceTests : IDisposable
//    {
//        private readonly Mock<ILogger<Project>> _mockLogger;
//        private readonly Mock<IMapper> _mockMapper;
//        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
//        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
//        private readonly ClaimRequestDbContext _dbContext;
//        private readonly ProjectService _projectService;

//        public DeleteProjectServiceTests()
//        {
//            _mockLogger = new Mock<ILogger<Project>>();
//            _mockMapper = new Mock<IMapper>();
//            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
//            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();

//            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
//                .UseInMemoryDatabase("DeleteProjectTestDb")
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
//        public async Task DeleteProject_ShouldReturnFalse_WhenProjectNotFound()
//        {
//            var id = Guid.NewGuid();
//            var repoMock = new Mock<IGenericRepository<Project>>();
//            repoMock.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<Project, bool>>>(), null, null))
//                    .ReturnsAsync((Project)null!);

//            _mockUnitOfWork.Setup(u => u.GetRepository<Project>()).Returns(repoMock.Object);
//            _mockUnitOfWork.Setup(u => u.Context.Database.CreateExecutionStrategy())
//                .Returns(_dbContext.Database.CreateExecutionStrategy());

//            var result = await _projectService.DeleteProject(id);

//            Assert.False(result);
//        }

//        public void Dispose()
//        {
//            _dbContext.Database.EnsureDeleted();
//            _mockHttpContextAccessor.Reset();
//            GC.SuppressFinalize(this);
//        }
//    }
//}