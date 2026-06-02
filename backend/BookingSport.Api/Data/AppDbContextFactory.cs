using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookingSport.Api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = ResolveBasePath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveBasePath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
        {
            return currentDirectory;
        }

        var projectDirectory = Path.Combine(currentDirectory, "BookingSport.Api");

        return File.Exists(Path.Combine(projectDirectory, "appsettings.json"))
            ? projectDirectory
            : currentDirectory;
    }
}
