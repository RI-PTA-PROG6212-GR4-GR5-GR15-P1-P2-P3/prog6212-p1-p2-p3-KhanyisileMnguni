using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using RaceDay.API.Data;

namespace RaceDay.API.Tests;

public sealed class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            // Remove the production SQL Server options configuration before adding
            // the isolated in-memory database used by these integration tests.
            var dbOptionsConfigurations = services
                .Where(descriptor =>
                    descriptor.ServiceType.IsGenericType &&
                    descriptor.ServiceType.GetGenericTypeDefinition().FullName ==
                        "Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration`1" &&
                    descriptor.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext))
                .ToList();

            foreach (var descriptor in dbOptionsConfigurations)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
