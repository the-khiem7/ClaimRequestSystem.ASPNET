#define DISABLE
#if !DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
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
    public class StaffServiceTests
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<Staff>> _mockStaffRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<StaffService>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly IStaffService _staffService;

        public StaffServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockStaffRepository = new Mock<IGenericRepository<Staff>>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<StaffService>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockUnitOfWork.Setup(u => u.GetRepository<Staff>()).Returns(_mockStaffRepository.Object);

            _staffService = new StaffService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object,
                _mockConfiguration.Object
            );
        }

        [Fact]
        public async Task CreateStaff_ShouldReturnResponse_WhenSuccessful()
        {
            // Arrange
            var request = new CreateStaffRequest { Email = "test@fpt.edu.vn", Password = "password123" };
            var staffEntity = new Staff { Id = Guid.NewGuid(), Email = request.Email, Password = "hashedpassword" };
            var response = new CreateStaffResponse { Id = staffEntity.Id, Email = staffEntity.Email };

            _mockMapper.Setup(m => m.Map<Staff>(request)).Returns(staffEntity);
            _mockMapper.Setup(m => m.Map<CreateStaffResponse>(staffEntity)).Returns(response);
            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<Staff, bool>>>(), null, null))
                .ReturnsAsync((Staff?)null); // No existing staff with same email
            _mockStaffRepository.Setup(r => r.InsertAsync(It.IsAny<Staff>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.ProcessInTransactionAsync(It.IsAny<Func<Task<CreateStaffResponse>>>()))
                .Returns((Func<Task<CreateStaffResponse>> operation) => operation());

            // Act
            var result = await _staffService.CreateStaff(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Email, result.Email);
        }

        [Fact]
        public async Task GetStaffById_ShouldReturnStaff_WhenExists()
        {
            
        }

        [Fact]
        public async Task GetStaffs_ShouldReturnListOfStaffs()
        {
            // Arrange
            var staffs = new List<Staff>
            {
                new Staff { Id = Guid.NewGuid(), Email = "staff1@fpt.edu.vn", IsActive = true },
                new Staff { Id = Guid.NewGuid(), Email = "staff2@fpt.edu.vn", IsActive = true }
            };
            _mockStaffRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Staff, bool>>>(), null, (Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>?)null))
                .ReturnsAsync(staffs);

            _mockMapper.Setup(m => m.Map<IEnumerable<CreateStaffResponse>>(staffs))
                .Returns(staffs.Select(s => new CreateStaffResponse { Id = s.Id, Email = s.Email }));

            // Act
            var result = await _staffService.GetStaffs();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task UpdateStaff_ShouldUpdate_WhenStaffExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateRequest = new UpdateStaffRequest { Email = "updated@fpt.edu.vn" };
            var existingStaff = new Staff { Id = id, Email = "old@fpt.edu.vn", IsActive = true };
            var updateResponse = new UpdateStaffResponse { Id = id, Email = updateRequest.Email };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<Staff, bool>>>(), null, null))
                .ReturnsAsync(existingStaff);
            _mockMapper.Setup(m => m.Map(updateRequest, existingStaff));
            _mockMapper.Setup(m => m.Map<UpdateStaffResponse>(existingStaff)).Returns(updateResponse);
            _mockStaffRepository.Setup(r => r.UpdateAsync(existingStaff));
            _mockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.ProcessInTransactionAsync(It.IsAny<Func<Task<UpdateStaffResponse>>>()))
                .Returns((Func<Task<UpdateStaffResponse>> operation) => operation());

            // Act
            var result = await _staffService.UpdateStaff(id, updateRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateRequest.Email, result.Email);
        }

        [Fact]
        public async Task DeleteStaff_ShouldSetInactive_WhenStaffExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var staff = new Staff { Id = id, Email = "delete@fpt.edu.vn", IsActive = true };

            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(It.IsAny<Expression<Func<Staff, bool>>>(), null, null))
                .ReturnsAsync(staff);
            _mockStaffRepository.Setup(r => r.UpdateAsync(staff));
            _mockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns((Func<Task<bool>> operation) => operation());

            // Act
            var result = await _staffService.DeleteStaff(id);

            // Assert
            Assert.True(result);
            Assert.False(staff.IsActive);
        }
    }
}
#endif