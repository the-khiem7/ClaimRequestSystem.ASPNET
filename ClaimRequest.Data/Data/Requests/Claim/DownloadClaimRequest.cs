using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Requests.Claim
{
    public class DownloadClaimRequest
    {
        public List<Guid> ClaimIds { get; set; }

    }
}
