//using System;
//using System.Collections.Generic;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using AutoMapper;
//using ClaimRequest.BLL.Services.Implements;
//using ClaimRequest.DAL.Data.Entities;
//using ClaimRequest.DAL.Data.Requests.Project;
//using ClaimRequest.DAL.Repositories.Interfaces;
//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Xunit;

//namespace ClaimRequest.UnitTest.Services
//{
//    public class CreateProjectServiceTests : IDisposable
//    {
//        private readonly Mock<ILogger<Project>> _mockLogger;
//        private readonly Mock<IMapper> _mockMapper;
//        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
//        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
//        private readonly ClaimRequestDbContext _realDbContext;
//        private readonly ProjectService _projectService;

//        public CreateProjectServiceTests()
//        {
//            _mockLogger = new Mock<ILogger<Project>>();
//            _mockMapper = new Mock<IMapper>();
//            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
//            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();

//            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
//                .UseInMemoryDatabase("ProjectCreateTestDb")
//                .Options;

//            _realDbContext = new ClaimRequestDbContext(options);
//            _realDbContext.Database.EnsureDeleted();
//            _realDbContext.Database.EnsureCreated();

//            _mockUnitOfWork.Setup(u => u.Context).Returns(_realDbContext);
//            _projectService = new ProjectService(
//                _mockUnitOfWork.Object,
//                _mockLogger.Object,
//                _mockMapper.Object,
//                _mockHttpContextAccessor.Object
//            );
//        }

//        [Fact]
//        public async Task CreateProject_ShouldThrowException_WhenProjectManagerInvalid()
//        {
//            var request = new CreateProjectRequest
//            {
//                Name = "Invalid Project",
//                ProjectManagerId = Guid.NewGuid(),
//                Status = ProjectStatus.Draft
//            };

//            var exception = await Assert.ThrowsAsync<Exception>(() => _projectService.CreateProject(request));
//            Assert.NotNull(exception);
//        }

//        public void Dispose()
//        {
//            _realDbContext.Database.EnsureDeleted();
//            GC.SuppressFinalize(this);
//        }
//    }
//}