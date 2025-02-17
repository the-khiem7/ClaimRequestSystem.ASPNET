using ClaimRequest.DAL.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class RejectClaimResponse
    {
        public Guid Id { get; set; }  // ID của Claim bị từ chối
        public ClaimStatus Status { get; set; } = ClaimStatus.Rejected;
        public string Remark { get; set; }  // Lý do từ chối
        public Guid ApproverId { get; set; }  // Người đã từ chối Claim
        public DateTime UpdateAt { get; set; }  // Thời điểm cập nhật trạng thái
    }
}
