using Microsoft.EntityFrameworkCore;

namespace ClaimRequest.DAL.Data.MetaDatas
{
    public static class PaginateExtentions
    {
        public static async Task<PagingResponse<T>> ToPagingResponse<T>(this IQueryable<T> query, int pageNumber, int pageSize, int firstPage = 1)
        {
            if (firstPage > pageNumber)
                throw new ArgumentException($"page ({pageNumber}) must greater or equal than firstPage ({firstPage})");

            var totalItems = query.Count();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagingResponse<T>
            {
                Items = items,
                Meta = new PaginationMeta
                {
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                }
            };
        }
    }
}
