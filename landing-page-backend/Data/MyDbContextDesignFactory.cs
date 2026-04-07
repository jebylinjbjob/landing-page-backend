using landing_page_backend.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace landing_page_backend
{
    public class MyDbContextDesignFactory : IDesignTimeDbContextFactory<MyDbContext>
    {
        public MyDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var config = BuildConfiguration(environment);

            var providerArg = args.FirstOrDefault(a => a.StartsWith("--provider=", StringComparison.OrdinalIgnoreCase));
            var provider = providerArg?.Split('=', 2)[1].Trim().ToLowerInvariant() ?? "mssql";

            if (provider == "postgresql" || provider == "postgres")
            {
                var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_POSTGRESQL")
                    ?? config.GetConnectionString("PostgreSql")
                    ?? "Host=localhost;Port=5432;Database=landing_page_backend;Username=postgres;Password=postgres";

                optionsBuilder.UseNpgsql(connectionString,
                    sql => sql.MigrationsAssembly(typeof(MyDbContext).Assembly.FullName));
            }
            else
            {
                var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_MSSQL")
                    ?? config.GetConnectionString("SqlServer")
                    ?? "Server=localhost,1433;Database=landing_page_backend;User Id=sa;Password=Your_password123;TrustServerCertificate=True";

                optionsBuilder.UseSqlServer(connectionString,
                    sql => sql.MigrationsAssembly(typeof(MyDbContext).Assembly.FullName));
            }

            return new MyDbContext(optionsBuilder.Options);
        }

        private static IConfigurationRoot BuildConfiguration(string environment)
        {
            var current = Directory.GetCurrentDirectory();
            var candidatePaths = new[]
            {
                current,
                Path.Combine(current, "landing-page-backend")
            };

            var basePath = candidatePaths.First(path => File.Exists(Path.Combine(path, "appsettings.json")));

            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
