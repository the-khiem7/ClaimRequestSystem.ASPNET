using ClaimRequest.DAL.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimRequest.API.Extensions
{
    public static class ApplyMigration
    {
        // This method is used to apply the migrations to the database when the application starts
        // will injected into the Program.cs file
        public static void ApplyMigrations(this IApplicationBuilder app)
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();
            using ClaimRequestDbContext context = scope.ServiceProvider.GetRequiredService<ClaimRequestDbContext>();

            try
            {
                var retryCount = 0;
                const int maxRetries = 5;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        context.Database.Migrate();
                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        if (retryCount == maxRetries)
                            throw;

                        // Wait before the next retry
                        Thread.Sleep(2000 * retryCount); // Exponential backoff
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ClaimRequestDbContext>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
                throw;
            }
        }
    }
}
