using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;
using Xunit.Abstractions;
using ClaimEntity = ClaimRequest.DAL.Data.Entities.Claim;


namespace ClaimRequest.UnitTest.Services
{
    public class SubmitClaimTests : IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<ClaimEntity>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<ClaimEntity>> _mockClaimRepository;
        private readonly ClaimService _claimService;
        private readonly Mock<IGenericRepository<ClaimApprover>> _mockClaimApproverRepository;
        private readonly Mock<IGenericRepository<ProjectStaff>> _mockProjectStaffRepository;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepository;
        private readonly Mock<IGenericRepository<ClaimChangeLog>> _mockClaimChangeLogRepository;

        public SubmitClaimTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<ClaimEntity>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<ClaimEntity>>();

            _mockClaimApproverRepository = new Mock<IGenericRepository<ClaimApprover>>();
            _mockProjectStaffRepository = new Mock<IGenericRepository<ProjectStaff>>();
            _mockProjectRepository = new Mock<IGenericRepository<Project>>();
            _mockClaimChangeLogRepository = new Mock<IGenericRepository<ClaimChangeLog>>();


            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimApprover>())
                .Returns(_mockClaimApproverRepository.Object);

            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimEntity>()).Returns(_mockClaimRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<ProjectStaff>()).Returns(_mockProjectStaffRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<Project>()).Returns(_mockProjectRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimChangeLog>())
                .Returns(_mockClaimChangeLogRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(operation => operation());


            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }


        public void Dispose() => GC.SuppressFinalize(this);

        [Fact]
        public async Task SubmitClaim_Should_SetStatusToPending_AndAssignApprover()
        {
            var claimId = Guid.NewGuid();
            var claimerId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Draft,
                ClaimerId = claimerId,
                ProjectId = projectId,
            };

            var project = new Project
            {
                Id = projectId,
                Name = "Test Project"
            };

            var approver = new ClaimApprover
            {
                ClaimId = claimId,
                ApproverId = approverId
            };

            var projectStaffs = new List<ProjectStaff>
            {
                new ProjectStaff
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Staff = new Staff
                    {
                        Id = approverId,
                        SystemRole = SystemRole.Approver,
                        IsActive = true,
                        Department = Department.ProjectManagement
                    },
                }
            };

            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", claimerId.ToString());
                        return null;
                    }
                );

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim);
            _mockProjectRepository
                .Setup(repo => repo.SingleOrDefaultAsync(It.IsAny<Expression<Func<Project, bool>>>(), null, null))
                .ReturnsAsync(
                    (Expression<Func<Project, bool>> predicate,
                        Func<IQueryable<Project>, IIncludableQueryable<Project, object>> include, bool noTracking) =>
                    {
                        return predicate.Compile()(project) ? project : null;
                    });

            _mockClaimRepository
                .Setup(repo => repo.SingleOrDefaultAsync(It.IsAny<Expression<Func<ClaimEntity, bool>>>(), null, null))
                .ReturnsAsync(claim);
            _mockClaimRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>()))
                .Verifiable();
            _mockClaimApproverRepository.Setup(repo => repo.InsertAsync(It.IsAny<ClaimApprover>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(operation => operation());
            _mockProjectStaffRepository
                .Setup(repo => repo.GetListAsync(
                    It.IsAny<Expression<Func<ProjectStaff, bool>>>(),
                    It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                    It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()
                ))
                .ReturnsAsync(projectStaffs);


            _mockClaimChangeLogRepository
                .Setup(repo => repo.InsertAsync(It.IsAny<ClaimChangeLog>()))
                .Returns(Task.CompletedTask);


            var result = await _claimService.SubmitClaim(claimId);

            Assert.True(result);
            Assert.Equal(ClaimStatus.Pending, claim.Status);
            _mockClaimRepository.Verify(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>()), Times.Once);
            _mockClaimApproverRepository.Verify(repo => repo.InsertAsync(It.IsAny<ClaimApprover>()), Times.Once);
        }

        [Fact]
        public async Task SubmitClaim_Should_ThrowNotFoundException_WhenClaimNotFound()
        {
            var claimerId = Guid.NewGuid();
            var claimId = Guid.NewGuid();
            var claim = new ClaimEntity
            {
                ClaimerId = claimerId,
            };
            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", claimerId.ToString());
                        return null;
                    }
                );
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync((ClaimEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _claimService.SubmitClaim(claimId));
        }

        [Fact]
        public async Task SubmitClaim_Should_ThrowBusinessException_WhenClaimNotDraftStatus()
        {
            var claimId = Guid.NewGuid();
            var claimerId = Guid.NewGuid();
            var claim = new ClaimEntity
            {
                Id = claimId,
                ClaimerId = claimerId,
                Status = ClaimStatus.Pending
            };

            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", claimerId.ToString());
                        return null;
                    }
                );

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            await Assert.ThrowsAsync<BusinessException>(() => _claimService.SubmitClaim(claimId));
        }

        [Fact]
        public async Task SubmitClaim_Should_ThrowNotFoundException_WhenApproverNotFound()
        {
            var claimId = Guid.NewGuid();
            var claimerId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Draft,
                ClaimerId = claimerId,
                ProjectId = projectId,
            };

            var project = new Project
            {
                Id = projectId,
                Name = "Test Project"
            };

            var projectStaffs = new List<ProjectStaff>();

            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", claimerId.ToString());
                        return null;
                    }
                );

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            _mockProjectRepository
                .Setup(repo => repo.SingleOrDefaultAsync(It.IsAny<Expression<Func<Project, bool>>>(), null, null))
                .ReturnsAsync(project);

            _mockProjectStaffRepository
                .Setup(repo => repo.GetListAsync(
                    It.IsAny<Expression<Func<ProjectStaff, bool>>>(),
                    It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                    It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()
                ))
                .ReturnsAsync(projectStaffs);

            await Assert.ThrowsAsync<NotFoundException>(() => _claimService.SubmitClaim(claimId));
        }

        [Fact]
        public async Task SubmitClaim_Should_ThrowUnauthorizedExceptionWhenClaimerIdNotLoggedUser()
        {
            var claimId = Guid.NewGuid();
            var claimerId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Draft,
                ClaimerId = claimerId,
                ProjectId = projectId,
            };
            var project = new Project
            {
                Id = projectId,
                Name = "Test Project"
            };
            var projectStaffs = new List<ProjectStaff>
            {
                new ProjectStaff
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Staff = new Staff
                    {
                        Id = Guid.NewGuid(),
                        SystemRole = SystemRole.Approver,
                        IsActive = true,
                        Department = Department.ProjectManagement
                    },
                }
            };
            _mockHttpContextAccessor
                .Setup(a => a.HttpContext.User.FindFirst(It.IsAny<string>()))
                .Returns(
                    (string claimType) =>
                    {
                        if (claimType == "StaffId")
                            return new System.Security.Claims.Claim("StaffId", Guid.NewGuid().ToString());
                        return null;
                    }
                );
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim);
            _mockProjectRepository
                .Setup(repo => repo.SingleOrDefaultAsync(It.IsAny<Expression<Func<Project, bool>>>(), null, null))
                .ReturnsAsync(project);
            _mockProjectStaffRepository
                .Setup(repo => repo.GetListAsync(
                    It.IsAny<Expression<Func<ProjectStaff, bool>>>(),
                    It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                    It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()
                ))
                .ReturnsAsync(projectStaffs);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _claimService.SubmitClaim(claimId));

        }
    }
}