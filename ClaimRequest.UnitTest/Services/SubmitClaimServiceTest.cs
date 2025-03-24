using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
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
        private readonly Mock<IGenericRepository<ClaimApprover>> _mockClaimApproverRepository; // Add this line

        public SubmitClaimTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<ClaimEntity>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<ClaimEntity>>();

            _mockClaimApproverRepository = new Mock<IGenericRepository<ClaimApprover>>();

            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimApprover>()).Returns(_mockClaimApproverRepository.Object);

            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimEntity>()).Returns(_mockClaimRepository.Object);
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
                UpdateAt = DateTime.UtcNow
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
            }
        }
    };
           

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim); 

            _mockClaimApproverRepository.Setup(repo => repo.InsertAsync(It.IsAny<ClaimApprover>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(uow => uow.GetRepository<ProjectStaff>()
                .GetListAsync(It.IsAny<Expression<Func<ProjectStaff, bool>>>(), null, null))
                .ReturnsAsync(projectStaffs);

            _mockUnitOfWork.Setup(uow => uow.GetRepository<Project>()
                .SingleOrDefaultAsync(It.IsAny<Expression<Func<Project, bool>>>(), null, null))
                .ReturnsAsync(project);

            _mockClaimRepository.Setup(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>()))
                .Callback<ClaimEntity>(c => { c.Status = ClaimStatus.Pending; });

            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(async func => await func());
            // Act
            var result = await _claimService.SubmitClaim(claimId);

            // Assert
            Assert.True(result);
            Assert.Equal(ClaimStatus.Pending, claim.Status);
            _mockClaimRepository.Verify(repo => repo.UpdateAsync(claim), Times.Once);
            _mockClaimApproverRepository.Verify(repo => repo.InsertAsync(It.IsAny<ClaimApprover>()), Times.Once);
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }
    }
    }
