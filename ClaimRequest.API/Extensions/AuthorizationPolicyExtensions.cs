using ClaimRequest.API.Constants;
using ClaimRequest.DAL.Data.Entities;
using Microsoft.AspNetCore.Authorization;

namespace ClaimRequest.API.Extensions
{
    public static class AuthorizationPolicyExtensions
    {
        public static void AddClaimRequestPolicies(this AuthorizationOptions options)
        {
            // Cac chinh sach dua tren vai tro
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(SystemRole.Admin.ToString()));
            options.AddPolicy("RequireFinanceRole", policy => policy.RequireRole(SystemRole.Finance.ToString()));
            options.AddPolicy("RequireApproverRole", policy => policy.RequireRole(SystemRole.Approver.ToString()));
            options.AddPolicy("RequireStaffRole", policy => policy.RequireRole(SystemRole.Staff.ToString()));
            
            // Chinh sach ket hop cac vai tro
            options.AddPolicy("RequireAnyRole", policy => 
                policy.RequireRole(RoleConstants.AllRoles));
            
            // Cac chinh sach cho Claim
            AddClaimPolicies(options);
            
            // Cac chinh sach cho quan ly
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
                policy.RequireRole(SystemRole.Staff.ToString()));
                
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
        
        private static void AddManagementPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("CanManageStaff", policy => 
                policy.RequireRole(SystemRole.Admin.ToString()));
                
            options.AddPolicy("CanManageProjects", policy => 
                policy.RequireRole(SystemRole.Admin.ToString()));
        }
    }
}