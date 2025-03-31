using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Project;
using ClaimRequest.DAL.Data.Responses.Project;
using ClaimRequest.DAL.Data.Responses.Staff;
using ClaimRequest.DAL.Repositories.Implements;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.UnitTests.Services.ProjectServiceTests
{
    public class CreateProjectTests : IDisposable
    {
        private readonly ClaimRequestDbContext _context;
        private readonly ProjectService _projectService;
        private readonly IMapper _mapper;
        private readonly ILogger<Project> _logger;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

        public CreateProjectTests()
        {
            // In-memory DB with suppressed transaction warnings
            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ClaimRequestDbContext(options);
            var unitOfWork = new UnitOfWork<ClaimRequestDbContext>(_context);

            // Add dummy project manager
            var projectManager = new Staff
            {
                Id = Guid.NewGuid(),
                Name = "Alice PM",
                Email = "alice@example.com",
                Password = "password",
                Department = Department.ProjectManagement,
                SystemRole = SystemRole.Admin,
                IsActive = true
            };
            _context.Staffs.Add(projectManager);
            _context.SaveChanges();

            // Setup mocks and mapper with full configuration
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CreateProjectRequest, Project>();
                cfg.CreateMap<Project, CreateProjectResponse>();
                cfg.CreateMap<Staff, CreateStaffResponse>();
                cfg.CreateMap<ProjectStaff, ProjectStaffResponse>();
            });

            _mapper = config.CreateMapper();
            _logger = Mock.Of<ILogger<Project>>();
            _projectService = new ProjectService(unitOfWork, _logger, _mapper, _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task Should_Create_Project_Successfully_When_Valid_Data()
        {
            // Arrange
            var projectManager = _context.Staffs.First();
            var request = new CreateProjectRequest
            {
                Name = "Test Project",
                Description = "Project for unit test",
                Budget = 10000,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                ProjectManagerId = projectManager.Id,
                Status = ProjectStatus.Draft
            };

            // Act
            var result = await _projectService.CreateProject(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Description, result.Description);
            Assert.Equal(request.Budget, result.Budget);
            Assert.Equal(request.Status, result.Status);
            Assert.NotNull(result.ProjectManager);
            Assert.Equal(projectManager.Id, result.ProjectManager.Id);
        }

        [Fact]
        public async Task Should_Throw_When_ProjectManager_NotFound()
        {
            // Arrange
            var missingId = Guid.NewGuid();
            var request = new CreateProjectRequest
            {
                Name = "Invalid Project",
                Description = "Should fail due to PM not found",
                Budget = 5000,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                ProjectManagerId = missingId,
                Status = ProjectStatus.Draft
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _projectService.CreateProject(request));
            Assert.Contains("invalid project manager", ex.Message, StringComparison.OrdinalIgnoreCase);
        }



        [Fact]
        public async Task Should_Throw_When_ProjectManager_IsInactive()
        {
            // Arrange
            var inactivePM = new Staff
            {
                Id = Guid.NewGuid(),
                Name = "Inactive PM",
                Email = "inactive@example.com",
                Password = "password",
                SystemRole = SystemRole.Admin,
                Department = Department.ProjectManagement,
                IsActive = false
            };
            _context.Staffs.Add(inactivePM);
            _context.SaveChanges();

            var request = new CreateProjectRequest
            {
                Name = "Inactive PM Project",
                Description = "Should fail due to inactive PM",
                Budget = 8000,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
                ProjectManagerId = inactivePM.Id,
                Status = ProjectStatus.Draft
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _projectService.CreateProject(request));
            Assert.Contains("not active", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Should_Throw_When_ProjectManager_Has_InvalidRole()
        {
            // Arrange
            var nonAdmin = new Staff
            {
                Id = Guid.NewGuid(),
                Name = "Non-Admin",
                Email = "nonadmin@example.com",
                Password = "password",
                SystemRole = SystemRole.Staff, // Not Admin
                Department = Department.ProjectManagement,
                IsActive = true
            };
            _context.Staffs.Add(nonAdmin);
            _context.SaveChanges();

            var request = new CreateProjectRequest
            {
                Name = "Wrong Role Project",
                Description = "Should fail due to invalid PM role",
                Budget = 12000,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
                ProjectManagerId = nonAdmin.Id,
                Status = ProjectStatus.Draft
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _projectService.CreateProject(request));
            Assert.Contains("can't be project mananger", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        //[Fact]
        //public async Task Should_Throw_When_Name_Exceeds_100_Chars()
        //{
        //    // Arrange
        //    var projectManager = _context.Staffs.First();
        //    var request = new CreateProjectRequest
        //    {
        //        Name = new string('A', 257), // 101 chars
        //        Description = "Valid description",
        //        StartDate = DateOnly.FromDateTime(DateTime.Today),
        //        EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
        //        Budget = 100,
        //        ProjectManagerId = projectManager.Id,
        //        Status = ProjectStatus.Draft
        //    };

        //    // Act & Assert
        //    await Assert.ThrowsAsync<ArgumentException>(() => _projectService.CreateProject(request));
        //}

        //[Fact]
        //public async Task Should_Throw_When_Description_Exceeds_1000_Chars()
        //{
        //    var projectManager = _context.Staffs.First();
        //    var request = new CreateProjectRequest
        //    {
        //        Name = "Valid name",
        //        Description = new string('D', 257),
        //        StartDate = DateOnly.FromDateTime(DateTime.Today),
        //        EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
        //        Budget = 100,
        //        ProjectManagerId = projectManager.Id,
        //        Status = ProjectStatus.Draft
        //    };

        //    await Assert.ThrowsAsync<ArgumentException>(() => _projectService.CreateProject(request));
        //}

        [Fact]
        public async Task Should_Throw_When_StartDate_Is_After_EndDate()
        {
            var projectManager = _context.Staffs.First();
            var request = new CreateProjectRequest
            {
                Name = "Valid name",
                Description = "Valid desc",
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                EndDate = DateOnly.FromDateTime(DateTime.Today),
                Budget = 100,
                ProjectManagerId = projectManager.Id,
                Status = ProjectStatus.Draft
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _projectService.CreateProject(request));
        }

        [Fact]
        public async Task Should_Create_When_StartDate_Equals_EndDate()
        {
            var projectManager = _context.Staffs.First();
            var today = DateOnly.FromDateTime(DateTime.Today);

            var request = new CreateProjectRequest
            {
                Name = "Same Date",
                Description = "Valid desc",
                StartDate = today,
                EndDate = today,
                Budget = 100,
                ProjectManagerId = projectManager.Id,
                Status = ProjectStatus.Draft
            };

            var result = await _projectService.CreateProject(request);
            Assert.NotNull(result);
            Assert.Equal(today, result.StartDate);
            Assert.Equal(today, result.EndDate);
        }

        //[Fact]
        //public async Task Should_Throw_When_Budget_Is_Negative()
        //{
        //    var projectManager = _context.Staffs.First();
        //    var request = new CreateProjectRequest
        //    {
        //        Name = "Negative budget",
        //        Description = "Valid desc",
        //        StartDate = DateOnly.FromDateTime(DateTime.Today),
        //        EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
        //        Budget = -1,
        //        ProjectManagerId = projectManager.Id,
        //        Status = ProjectStatus.Draft
        //    };

        //    await Assert.ThrowsAsync<ArgumentException>(() => _projectService.CreateProject(request));
        //}

        [Fact]
        public async Task Should_Create_When_Budget_Is_Zero()
        {
            var projectManager = _context.Staffs.First();
            var request = new CreateProjectRequest
            {
                Name = "Zero budget",
                Description = "Valid desc",
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                Budget = 0,
                ProjectManagerId = projectManager.Id,
                Status = ProjectStatus.Draft
            };

            var result = await _projectService.CreateProject(request);
            Assert.Equal(0, result.Budget);
        }

        [Fact]
        public async Task Should_Create_When_Budget_Is_Positive()
        {
            var projectManager = _context.Staffs.First();
            var request = new CreateProjectRequest
            {
                Name = "Positive budget",
                Description = "Valid desc",
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                Budget = 1,
                ProjectManagerId = projectManager.Id,
                Status = ProjectStatus.Draft
            };

            var result = await _projectService.CreateProject(request);
            Assert.Equal(1, result.Budget);
        }



        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
