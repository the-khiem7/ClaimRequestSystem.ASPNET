using ClaimRequest.DAL.Data.Entities;
using System.Linq;

namespace ClaimRequest.API.Constants
{
    public static class RoleConstants
    {
        public static readonly string[] AllRoles = Enum.GetValues(typeof(SystemRole))
                                                      .Cast<SystemRole>()
                                                      .Select(r => r.ToString())
                                                      .ToArray();
    }
}