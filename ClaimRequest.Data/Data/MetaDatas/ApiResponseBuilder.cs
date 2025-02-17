namespace ClaimRequest.DAL.Data.MetaDatas
{
    public class ApiResponseBuilder
    {

        // This method is used to build a response object for single data
        public static ApiResponse<T> BuildResponse<T>(int statusCode, string message, T data, string reason = null)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                Message = message,
                Data = data,
                IsSuccess = statusCode >= 200 && statusCode < 300,
                Reason = reason
            };
        }

        // This method is used to build a response object for error response
        public static ApiResponse<T> BuildErrorResponse<T>(T data, int statusCode, string message, string reason)
        {
            return new ApiResponse<T>
            {
                Data = data,
                StatusCode = statusCode,
                Message = message,
                Reason = reason,
                IsSuccess = false
                //StatusCode = statusCode,
                //Message = message,
                //Data = null,
                //IsSuccess = false,
                //Reason = reason
            };
        }

        // This method is used to build a response object for list/pagination data
        public static ApiResponse<PagingResponse<T>> BuildPageResponse<T>(
            IEnumerable<T> items,
            int totalPages,
            int currentPage,
            int pageSize,
            long totalItems,
            string message)
        {
            var pagedResponse = new PagingResponse<T>
            {
                Items = items,
                Meta = new PaginationMeta
                {
                    TotalPages = totalPages,
                    CurrentPage = currentPage,
                    PageSize = pageSize,
                    TotalItems = totalItems
                }
            };

            return new ApiResponse<PagingResponse<T>>
            {
                Data = pagedResponse,
                Message = message,
                StatusCode = 200,
                IsSuccess = true,
                Reason = null
            };
        }


        //// API Response Wrapper
        //public class ApiResponse<T>
        //{
        //    public int StatusCode { get; set; }
        //    public string Message { get; set; }
        //    public T Data { get; set; }
        //    public bool IsSuccess { get; set; }
        //    public string Reason { get; set; }
        //}

        //// Pagination Wrapper
        //public class PagingResponse<T>
        //{
        //    public IEnumerable<T> Items { get; set; }
        //    public PaginationMeta Meta { get; set; }
        //}

        //// Pagination MetaData
        //public class PaginationMeta
        //{
        //    public int TotalPages { get; set; }
        //    public int CurrentPage { get; set; }
        //    public int PageSize { get; set; }
        //    public long TotalItems { get; set; }
        //}
    }
}
