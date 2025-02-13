using AutoMapper;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Staff;
using ClaimRequest.DAL.Data.Responses.Staff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Mappers
{
    public class StaffMapper : Profile
    {
        public StaffMapper()
        {
            // CreateStaffRequest -> Staff
            CreateMap<CreateStaffRequest, Staff>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.ProjectStaffs, opt => opt.Ignore());

            // Staff -> CreateStaffResponse
            CreateMap<Staff, CreateStaffResponse>();
        }
    }
}
