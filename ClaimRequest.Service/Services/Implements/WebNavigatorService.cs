using System.Security.Claims;
using System.Text.Json;
using ClaimRequest.BLL.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Responses;

namespace ClaimRequest.BLL.Services.Implements
{
    public class WebNavigatorService : IWebNavigatorService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebNavigatorService> _logger;
        public WebNavigatorService(IConfiguration configuration, ILogger<WebNavigatorService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetSidebarElement()
        {
            var userRole = ClaimsPrincipal.Current.FindFirst("Role")?.Value;

            if (string.IsNullOrEmpty(userRole))
                throw new UnauthorizedAccessException("Invalid token or missing role.");

            List<SidebarElement> sidebarElements = userRole switch
            {
                nameof(SystemRole.Staff) => GetStaffSidebarElement(),
                nameof(SystemRole.Approver) => GetApproverSidebarElement(),
                nameof(SystemRole.Finance) => GetFinanceSidebarElement(),
                nameof(SystemRole.Admin) => GetAdminSidebarElement(),
                _ => throw new UnauthorizedAccessException("Invalid role.")
            };

            return JsonSerializer.Serialize(sidebarElements);
        }

        private List<SidebarElement> GetStaffSidebarElement()
        {
            _logger.LogInformation("Staff sidebar elements retrieved.");
            return new List<SidebarElement>();
        }

        private List<SidebarElement> GetApproverSidebarElement()
        {
            _logger.LogInformation("Approver sidebar elements retrieved.");
            return new List<SidebarElement>
                    {
                        new SidebarElement { Path = "/approval/vetting", Layout = "ApproverLayout", Component = "Approve" },
                        new SidebarElement { Path = "/detail/:id", Layout = "ApproverLayout", Component = "Detail" }
                    };
        }

        private List<SidebarElement> GetFinanceSidebarElement()
        {
            _logger.LogInformation("Finance sidebar elements retrieved.");
            return new List<SidebarElement>
                    {
                        new SidebarElement { Path = "/finance/approved", Layout = "FinanceLayout", Component = "FinanceRequest" },
                        new SidebarElement { Path = "/finance/detail/:id", Layout = "FinanceLayout", Component = "FinanceDetail" }
                    };
        }

        private List<SidebarElement> GetAdminSidebarElement()
        {
            _logger.LogInformation("Admin sidebar elements retrieved.");
            return new List<SidebarElement>
                    {
                        new SidebarElement { Path = "/admin/projects", Layout = "AdminLayout", Component = "ProjectList" },
                        new SidebarElement { Path = "/admin/staffs", Layout = "AdminLayout", Component = "StaffList" }
                    };
        }
    }
}
