using System.Linq.Expressions;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Staff;
using ClaimRequest.DAL.Data.Responses.Staff;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.Tests.Services
{
    public class AssignRemoveStaffTests : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<Staff>> _mockStaffRepository;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepository;
        private readonly Mock<IGenericRepository<ProjectStaff>> _mockProjectStaffRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<StaffService>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ICloudinaryService> _mockCloudinaryService;
        private readonly IStaffService _staffService;

        public AssignRemoveStaffTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockStaffRepository = new Mock<IGenericRepository<Staff>>();
            _mockProjectRepository = new Mock<IGenericRepository<Project>>();
            _mockProjectStaffRepository = new Mock<IGenericRepository<ProjectStaff>>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<StaffService>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockCloudinaryService = new Mock<ICloudinaryService>();

            _mockUnitOfWork.Setup(u => u.GetRepository<Staff>()).Returns(_mockStaffRepository.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Project>()).Returns(_mockProjectRepository.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<ProjectStaff>()).Returns(_mockProjectStaffRepository.Object);

            // Setup ProcessInTransactionAsync to execute the operation directly
            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<AssignStaffResponse>>>()))
                .Returns<Func<Task<AssignStaffResponse>>>(operation => operation());
            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<RemoveStaffResponse>>>()))
                .Returns<Func<Task<RemoveStaffResponse>>>(operation => operation());

            _staffService = new StaffService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object,
                _mockConfiguration.Object,
                _mockCloudinaryService.Object
            );
        }

        public void Dispose() => GC.SuppressFinalize(this);

        //Assign staff into project
        [Fact]
        public async Task AssignStaff_ShouldReturnResponse_WhenSuccessful()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var assignerId = Guid.NewGuid();

            var request = new AssignStaffRequest
            {
                projectId = projectId,
                AssignerId = assignerId,
                ProjectRole = ProjectRole.Developer
            };

            var staff = new Staff { Id = staffId, IsActive = true };
            var project = new Project { Id = projectId };
            var assigner = new ProjectStaff
            {
                StaffId = assignerId,
                ProjectId = projectId,
                ProjectRole = ProjectRole.ProjectManager
            };

            var newProjectStaff = new ProjectStaff
            {
                Id = Guid.NewGuid(),
                StaffId = staffId,
                ProjectId = projectId,
                ProjectRole = ProjectRole.Developer
            };

            var expectedResponse = new AssignStaffResponse
            {
                Id = newProjectStaff.Id,
                StaffId = staffId,
                projectId = projectId,
                ProjectRole = ProjectRole.Developer
            };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            _mockProjectRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IOrderedQueryable<Project>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
                .ReturnsAsync(project);

            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<ProjectStaff, bool>>>(expr =>
                    expr.Compile().Invoke(new ProjectStaff
                    {
                        StaffId = assignerId,
                        ProjectId = projectId
                    })),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(assigner);

            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<ProjectStaff, bool>>>(expr =>
                    expr.Compile().Invoke(new ProjectStaff
                    {
                        StaffId = staffId,
                        ProjectId = projectId
                    })),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync((ProjectStaff)null);

            _mockMapper.Setup(m => m.Map<ProjectStaff>(request)).Returns(newProjectStaff);
            _mockMapper.Setup(m => m.Map<AssignStaffResponse>(newProjectStaff)).Returns(expectedResponse);

            _mockProjectStaffRepository.Setup(r => r.InsertAsync(It.IsAny<ProjectStaff>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _staffService.AssignStaff(staffId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(staffId, result.StaffId);
            Assert.Equal(projectId, result.projectId);
            Assert.Equal(ProjectRole.Developer, result.ProjectRole);

            _mockProjectStaffRepository.Verify(repo => repo.InsertAsync(It.IsAny<ProjectStaff>()), Times.Once);
            _mockMapper.Verify(m => m.Map<ProjectStaff>(request), Times.Once);
            _mockMapper.Verify(m => m.Map<AssignStaffResponse>(It.IsAny<ProjectStaff>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<AssignStaffResponse>>>()), Times.Once);
        }

        [Fact]
        public async Task AssignStaff_ShouldThrowException_WhenStaffNotFound()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var assignerId = Guid.NewGuid();

            var request = new AssignStaffRequest
            {
                projectId = projectId,
                AssignerId = assignerId,
                ProjectRole = ProjectRole.Developer
            };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync((Staff)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
                _staffService.AssignStaff(staffId, request));
            Assert.Equal($"Staff with ID {staffId} are not found", exception.Message);
        }

        [Fact]
        public async Task AssignStaff_ShouldThrowException_WhenProjectNotFound()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var assignerId = Guid.NewGuid();

            var request = new AssignStaffRequest
            {
                projectId = projectId,
                AssignerId = assignerId,
                ProjectRole = ProjectRole.Developer
            };

            var staff = new Staff { Id = staffId, IsActive = true };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            _mockProjectRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IOrderedQueryable<Project>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
                .ReturnsAsync((Project)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
                _staffService.AssignStaff(staffId, request));
            Assert.Equal("Project not found.", exception.Message);
        }

        [Fact]
        public async Task AssignStaff_ShouldThrowException_WhenAssignerNotProjectManager()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var assignerId = Guid.NewGuid();

            var request = new AssignStaffRequest
            {
                projectId = projectId,
                AssignerId = assignerId,
                ProjectRole = ProjectRole.Developer
            };

            var staff = new Staff { Id = staffId, IsActive = true };
            var project = new Project { Id = projectId };
            var assigner = new ProjectStaff
            {
                StaffId = assignerId,
                ProjectId = projectId,
                ProjectRole = ProjectRole.Developer // Not a Project Manager
            };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            _mockProjectRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IOrderedQueryable<Project>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
                .ReturnsAsync(project);

            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ProjectStaff, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(assigner);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _staffService.AssignStaff(staffId, request));
            Assert.Equal("You do not have permission to assign staff to this project.", exception.Message);
        }

        [Fact]
        public async Task AssignStaff_ShouldThrowException_WhenStaffAlreadyInProject()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var assignerId = Guid.NewGuid();

            var request = new AssignStaffRequest
            {
                projectId = projectId,
                AssignerId = assignerId,
                ProjectRole = ProjectRole.Developer
            };

            var staff = new Staff { Id = staffId, IsActive = true };
            var project = new Project { Id = projectId };
            var assigner = new ProjectStaff
            {
                StaffId = assignerId,
                ProjectId = projectId,
                ProjectRole = ProjectRole.ProjectManager
            };
            var existingProjectStaff = new ProjectStaff
            {
                StaffId = staffId,
                ProjectId = projectId,
                ProjectRole = ProjectRole.Developer
            };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            _mockProjectRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IOrderedQueryable<Project>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
                .ReturnsAsync(project);

            // Setup for assigner check - should return a project manager
            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<ProjectStaff, bool>>>(expr =>
                    expr.Compile().Invoke(new ProjectStaff { StaffId = assignerId, ProjectId = projectId })),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(assigner);

            // Setup for staff check - should return existing project staff
            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<ProjectStaff, bool>>>(expr =>
                    expr.Compile().Invoke(new ProjectStaff { StaffId = staffId, ProjectId = projectId })),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(existingProjectStaff);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                _staffService.AssignStaff(staffId, request));
            Assert.Equal("Staff already assign to this project.", exception.Message);
        }


        // Remove staff from project
        [Fact]
        public async Task RemoveStaff_ShouldReturnResponse_WhenSuccessful()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var removerId = Guid.NewGuid();

            var request = new RemoveStaffRequest
            {
                projectId = projectId,
                RemoverId = removerId
            };

            var staff = new Staff { Id = staffId, IsActive = true };
            var project = new Project { Id = projectId };
            var remover = new Staff { Id = removerId, SystemRole = SystemRole.Admin }; // Admin can remove staff
            var projectStaff = new ProjectStaff
            {
                Id = Guid.NewGuid(),
                StaffId = staffId,
                ProjectId = projectId,
                ProjectRole = ProjectRole.Developer
            };

            var expectedResponse = new RemoveStaffResponse
            {
                Id = projectStaff.Id,
                StaffId = staffId,
                projectId = projectId,
                ProjectRole = ProjectRole.Developer
            };

            // Setup staff validation
            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<Staff, bool>>>(expr =>
                    expr.Compile().Invoke(new Staff { Id = staffId, IsActive = true })),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            // Setup project validation
            _mockProjectRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<Project, bool>>>(expr =>
                    expr.Compile().Invoke(new Project { Id = projectId })),
                It.IsAny<Func<IQueryable<Project>, IOrderedQueryable<Project>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
                .ReturnsAsync(project);

            // Setup remover validation
            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<Staff, bool>>>(expr =>
                    expr.Compile().Invoke(new Staff { Id = removerId })),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(remover);

            // Setup project staff validation
            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<ProjectStaff, bool>>>(expr =>
                    expr.Compile().Invoke(new ProjectStaff { StaffId = staffId, ProjectId = projectId })),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(projectStaff);

            _mockMapper.Setup(m => m.Map<RemoveStaffResponse>(projectStaff)).Returns(expectedResponse);

            // Act
            var result = await _staffService.RemoveStaff(staffId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(staffId, result.StaffId);
            Assert.Equal(projectId, result.projectId);
            Assert.Equal(ProjectRole.Developer, result.ProjectRole);

            _mockProjectStaffRepository.Verify(repo => repo.DeleteAsync(It.IsAny<ProjectStaff>()), Times.Once);
            _mockMapper.Verify(m => m.Map<RemoveStaffResponse>(It.IsAny<ProjectStaff>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<RemoveStaffResponse>>>()), Times.Once);
        }

        [Fact]
        public async Task RemoveStaff_ShouldThrowException_WhenStaffNotFound()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var removerId = Guid.NewGuid();

            var request = new RemoveStaffRequest
            {
                projectId = projectId,
                RemoverId = removerId
            };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync((Staff)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
                _staffService.RemoveStaff(staffId, request));
            Assert.Equal($"Staff with ID {staffId} are not found", exception.Message);
        }

        [Fact]
        public async Task RemoveStaff_ShouldThrowException_WhenProjectNotFound()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var assignerId = Guid.NewGuid();

            var request = new RemoveStaffRequest
            {
                projectId = projectId,
                RemoverId = assignerId,
            };

            var staff = new Staff { Id = staffId, IsActive = true };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            _mockProjectRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IOrderedQueryable<Project>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
                .ReturnsAsync((Project)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
                _staffService.RemoveStaff(staffId, request));
            Assert.Equal("Project not found.", exception.Message);
        }

        [Fact]
        public async Task RemoveStaff_ShouldThrowException_WhenRemoverNotProjectManager()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var removerId = Guid.NewGuid();

            var request = new RemoveStaffRequest
            {
                projectId = projectId,
                RemoverId = removerId
            };

            var staff = new Staff { Id = staffId, IsActive = true };
            var project = new Project { Id = projectId };
            var remover = new Staff { Id = removerId, SystemRole = SystemRole.Staff }; // Not an admin
            var projectStaff = new ProjectStaff
            {
                StaffId = staffId,
                ProjectId = projectId,
                ProjectRole = ProjectRole.Developer
            };

            // Setup staff validation
            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<Staff, bool>>>(expr =>
                    expr.Compile().Invoke(new Staff { Id = staffId, IsActive = true })),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            // Setup project validation
            _mockProjectRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<Project, bool>>>(expr =>
                    expr.Compile().Invoke(new Project { Id = projectId })),
                It.IsAny<Func<IQueryable<Project>, IOrderedQueryable<Project>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
                .ReturnsAsync(project);

            // Setup remover validation
            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<Staff, bool>>>(expr =>
                    expr.Compile().Invoke(new Staff { Id = removerId })),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(remover);

            // Setup project staff validation - remover is not in project
            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<ProjectStaff, bool>>>(expr =>
                    expr.Compile().Invoke(new ProjectStaff { StaffId = removerId, ProjectId = projectId })),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync((ProjectStaff)null);

            // Setup staff-in-project check
            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<ProjectStaff, bool>>>(expr =>
                    expr.Compile().Invoke(new ProjectStaff { StaffId = staffId, ProjectId = projectId })),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(projectStaff);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                _staffService.RemoveStaff(staffId, request));
            Assert.Equal("You are not a member of this project.", exception.Message);
        }

        [Fact]
        public async Task RemoveStaff_ShouldThrowException_WhenStaffNotInProject()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var removerId = Guid.NewGuid();

            var request = new RemoveStaffRequest
            {
                projectId = projectId,
                RemoverId = removerId
            };

            var staff = new Staff { Id = staffId, IsActive = true };
            var project = new Project { Id = projectId };
            var remover = new Staff { Id = removerId, SystemRole = SystemRole.Admin }; // Admin can remove staff

            // Setup staff validation
            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<Staff, bool>>>(expr =>
                    expr.Compile().Invoke(new Staff { Id = staffId, IsActive = true })),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            // Setup project validation
            _mockProjectRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<Project, bool>>>(expr =>
                    expr.Compile().Invoke(new Project { Id = projectId })),
                It.IsAny<Func<IQueryable<Project>, IOrderedQueryable<Project>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
                .ReturnsAsync(project);

            // Setup remover validation
            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<Staff, bool>>>(expr =>
                    expr.Compile().Invoke(new Staff { Id = removerId })),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(remover);

            // Setup staff-in-project check - staff is not in project
            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<ProjectStaff, bool>>>(expr =>
                    expr.Compile().Invoke(new ProjectStaff { StaffId = staffId, ProjectId = projectId })),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync((ProjectStaff)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                _staffService.RemoveStaff(staffId, request));
            Assert.Equal("Staff is not assign to this project.", exception.Message);
        }

        [Fact]
        public async Task RemoveStaff_ShouldThrowException_WhenRemovingProjectManager()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var removerId = Guid.NewGuid();

            var request = new RemoveStaffRequest
            {
                projectId = projectId,
                RemoverId = removerId
            };

            var staff = new Staff { Id = staffId, IsActive = true };
            var project = new Project { Id = projectId };
            var remover = new ProjectStaff
            {
                StaffId = removerId,
                ProjectId = projectId,
                ProjectRole = ProjectRole.ProjectManager
            };
            var projectStaff = new ProjectStaff
            {
                StaffId = staffId,
                ProjectId = projectId,
                ProjectRole = ProjectRole.ProjectManager // Trying to remove a Project Manager
            };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            _mockProjectRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IOrderedQueryable<Project>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
                .ReturnsAsync(project);

            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<ProjectStaff, bool>>>(expr => expr.ToString().Contains("RemoverId")),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(remover);

            _mockProjectStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.Is<Expression<Func<ProjectStaff, bool>>>(expr => expr.ToString().Contains("StaffId")),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(projectStaff);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                _staffService.RemoveStaff(staffId, request));
            Assert.Equal("Project Manager cannot be remove from project.", exception.Message);
        }

    }
}
