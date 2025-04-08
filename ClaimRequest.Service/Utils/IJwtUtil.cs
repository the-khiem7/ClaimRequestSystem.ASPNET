using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.BLL.Utils
{
    public interface IJwtUtil
    {
        string GenerateJwtToken(Staff staff, Tuple<string, Guid> tuple, bool flag);
    }
}
