using ClaimRequest.DAL.Data.Responses.Staff;

namespace ClaimRequest.DAL.Data.Responses.Paging
{
    public class PagingResponse<T>
    {
        public IEnumerable<T> Items { get; set; }
        public PaginationMeta Meta { get; set; }

        public static implicit operator PagingResponse<T>(PagingResponse<CreateStaffResponse> v)
        {
            throw new NotImplementedException();
        }
    }

    public class PaginationMeta
    {
        public int TotalPages { get; set; }
        public long TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
