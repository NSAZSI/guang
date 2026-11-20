using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdPulse.Api.Data;

/// <summary>dotnet-ef 设计时工厂：迁移生成不依赖运行时宿主。</summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        var conn = config.GetConnectionString("Default") ?? "Data Source=App_Data/adpulse.db";
        var source = conn.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (!Path.IsPathRooted(source)) source = Path.GetFullPath(source);
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={source}").Options;
        return new AppDbContext(options);
    }
}