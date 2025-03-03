using ClaimRequest.DAL.Data.Exceptions;

namespace ClaimRequest.BLL.Extension
{
    public static class EntityValidationExtensions
    {
        // Xem chi tiet trong Notion tui vua lam: https://khiemnvd.notion.site/Extension-Method-ValidateExist-1a5600ddb8ea80189bf5cca76ebaf697?pvs=4


        /// <summary>
        /// Check enitty co ton tai ko, neu ton tai thi tra ve entity, nguoc lai throw exception
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="customMessage"></param>
        /// <param name="entityName"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public static TEntity ValidateExists<TEntity>(
            this TEntity? entity,           // dua entity can check vao
            Guid? id = null,                // neu muon output hien id của entity
            string? entityName = null,      // tu dat Ten object neu can, neu ko truyen thi lay ten cua entity
            string? customMessage = null)   // chen thong bao loi hoac bo trong neu muon xai thong bao tui tao san o duoi 
            where TEntity : class
        {
            if (entity == null)
            {
                var typeName = entityName ?? typeof(TEntity).Name; //tu dong get ten cua entity neu ko truyen vao
                var message = customMessage ?? $"{typeName}{(id.HasValue ? $" with ID {id}" : "")} are not found";
                throw new NotFoundException(message);
            }
            return entity;
        }
    }
}
