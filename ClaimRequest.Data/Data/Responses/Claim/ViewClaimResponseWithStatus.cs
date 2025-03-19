using ClaimRequest.DAL.Data.MetaDatas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class ViewClaimResponseWithStatus
    {
        public PagingResponse<ViewClaimResponse>? Claims { get; set; }
        public StatusCounts? StatusCounts { get; set; }
    }
}
