using ClaimRequest.API.Constants;
using ClaimRequest.DAL.Data.Entities;
using Microsoft.AspNetCore.Authorization;

namespace ClaimRequest.API.Extensions
{
    public static class AuthorizationPolicyExtensions
    {
        public static void AddClaimRequestPolicies(this AuthorizationOptions options)
        {
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(SystemRole.Admin.ToString()));
            options.AddPolicy("RequireFinanceRole", policy => policy.RequireRole(SystemRole.Finance.ToString()));
            options.AddPolicy("RequireApproverRole", policy => policy.RequireRole(SystemRole.Approver.ToString()));
            options.AddPolicy("RequireStaffRole", policy => policy.RequireRole(SystemRole.Staff.ToString()));
            options.AddPolicy("RequireAnyRole", policy => policy.RequireRole(RoleConstants.AllRoles));
            
            AddClaimPolicies(options);

            AddProjectPolicies(options);

            AddManagementPolicies(options);
        }
        
        private static void AddClaimPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("CanCreateClaim", policy => 
                policy.RequireRole(SystemRole.Staff.ToString()));
                
            options.AddPolicy("CanViewClaims", policy => 
                policy.RequireRole(RoleConstants.AllRoles));
                
            options.AddPolicy("CanUpdateClaim", policy => 
                policy.RequireRole(SystemRole.Staff.ToString()));
                
            options.AddPolicy("CanSubmitClaim", policy =>
                policy.RequireRole(RoleConstants.AllRoles));

            options.AddPolicy("CanApproveClaim", policy => 
                policy.RequireRole(SystemRole.Approver.ToString()));
                
            options.AddPolicy("CanRejectClaim", policy => 
                policy.RequireRole(SystemRole.Approver.ToString()));
                
            options.AddPolicy("CanReturnClaim", policy => 
                policy.RequireRole(SystemRole.Approver.ToString(), SystemRole.Finance.ToString()));
                
            options.AddPolicy("CanCancelClaim", policy => 
                policy.RequireRole(SystemRole.Staff.ToString()));
                
            options.AddPolicy("CanProcessPayment", policy => 
                policy.RequireRole(SystemRole.Finance.ToString()));
                
            options.AddPolicy("CanDownloadClaim", policy => 
                policy.RequireRole(SystemRole.Finance.ToString()));
        }

        private static void AddProjectPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("CanCreateProject", policy =>
                policy.RequireRole(SystemRole.Admin.ToString()));

            options.AddPolicy("CanViewProjects", policy =>
                policy.RequireRole(RoleConstants.AllRoles));

            options.AddPolicy("CanUpdateProject", policy =>
                policy.RequireRole(SystemRole.Admin.ToString()));

            options.AddPolicy("CanDeleteProject", policy =>
                policy.RequireRole(SystemRole.Admin.ToString()));
            

            options.AddPolicy("CanAssignProjectManager", policy =>
                policy.RequireRole(SystemRole.Admin.ToString(), SystemRole.Approver.ToString()));

            options.AddPolicy("CanAssignProjectRole", policy =>
                policy.RequireRole(SystemRole.Admin.ToString(), SystemRole.Approver.ToString()));

            options.AddPolicy("CanAssignProjectMember", policy =>
                policy.RequireRole(SystemRole.Admin.ToString(), SystemRole.Approver.ToString()));

            options.AddPolicy("CanRemoveProjectMember", policy =>
                policy.RequireRole(SystemRole.Admin.ToString(), SystemRole.Approver.ToString()));

            options.AddPolicy("CanViewProjectMembers", policy =>
                policy.RequireRole(RoleConstants.AllRoles));
        }


        private static void AddManagementPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("CanManageStaff", policy => 
                policy.RequireRole(SystemRole.Admin.ToString(), SystemRole.Staff.ToString(), SystemRole.Approver.ToString()));
                
            options.AddPolicy("CanManageProjects", policy => 
                policy.RequireRole(SystemRole.Admin.ToString()));
        }
    }
}