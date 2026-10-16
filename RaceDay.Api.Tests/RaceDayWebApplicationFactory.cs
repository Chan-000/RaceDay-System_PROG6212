using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.API.Models;
using RaceDay.API.Data;
using Microsoft.AspNetCore.Hosting;

namespace RaceDay.Api.Tests
{
    public class RaceDayWebApplicationFactory : WebApplicationFactory<Program> 
    {
        private readonly string _databaseName = "RaceDayTests" + Guid.NewGuid().ToString("N");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<RaceDayDbContext>();
                services.RemoveAll<DbContextOptions<RaceDayDbContext>>();

                services.AddDbContext<RaceDayDbContext>(options =>
                   options.UseInMemoryDatabase(_databaseName));
            });
        }

        public async Task SeedDatabaseAsync()
        {
            using var scope = Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

            await context.Database.EnsureCreatedAsync();

            if (!await context.EventTypes.AnyAsync())
            {
                context.EventTypes.Add(new EventType
                {
                    TypeName = "Running",
                    Description = "Road running events"
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
