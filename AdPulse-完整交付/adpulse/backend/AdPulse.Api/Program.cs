using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AdPulse.Api.Data;
using AdPulse.Api.Filters;
using AdPulse.Api.Middlewares;
using AdPulse.Api.Models;
using AdPulse.Api.Repositories;
using AdPulse.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Serilog 日志路径：生产用环境变量 Serilog__Path 指向 /var/adpulse/logs
var serilogPath = Environment.GetEnvironmentVariable("Serilog__Path") ?? "logs/adpulse-.log";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(serilogPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    // ---------- 运维命令：重置用户密码（部署时改掉演示账号默认密码用） ----------
    // 用法：dotnet AdPulse.Api.dll --reset-password <用户名> <新密码>
    if (args.Length >= 3 && args[0] == "--reset-password")
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? "Data Source=App_Data/adpulse.db";
        var ds = cs.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (!Path.IsPathRooted(ds)) ds = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ds));
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={ds}").Options;
        using var db = new AppDbContext(opts);
        var user = db.Users.FirstOrDefault(u => u.UserName == args[1]);
        if (user is null) { Console.Error.WriteLine($"用户 {args[1]} 不存在"); return 1; }
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, args[2]);
        db.SaveChanges();
        Console.WriteLine($"已重置 {args[1]} 的密码（PBKDF2 哈希入库，明文不落盘）");
        return 0;
    }

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ---------- 数据库：SQLite，连接串强制解析为绝对路径（计划 2.5） ----------
    var contentRoot = builder.Environment.ContentRootPath;
    var connStr = builder.Configuration.GetConnectionString("Default") ?? "Data Source=App_Data/adpulse.db";
    var dataSource = connStr.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();
    if (!Path.IsPathRooted(dataSource))
        dataSource = Path.GetFullPath(Path.Combine(contentRoot, dataSource));
    Directory.CreateDirectory(Path.GetDirectoryName(dataSource)!);
    var absoluteConn = $"Data Source={dataSource}";

    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(absoluteConn));

    // ---------- MVC + 统一校验过滤器 + JSON 约定 ----------
    builder.Services.AddControllers(o => o.Filters.Add<ValidationFilter>())
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    builder.Services.AddCors(o => o.AddPolicy("DevCors", p =>
        p.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173").AllowAnyHeader().AllowAnyMethod()));

    // ---------- JWT 认证 + 角色授权（计划 2.6） ----------
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
    {
        if (builder.Environment.IsDevelopment())
            jwtKey = "AdPulse-Dev-Only-Signing-Key-Change-In-Production-0123456789";
        else
            throw new InvalidOperationException("生产环境必须配置 Jwt:Key");
    }
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = "AdPulse",
            ValidateAudience = true, ValidAudience = "AdPulseWeb",
            ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        o.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { code = 40100, message = "未登录或登录已过期", data = (object?)null }));
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { code = 40300, message = "权限不足，无法执行该操作", data = (object?)null }));
            }
        };
    });
    builder.Services.AddAuthorization(o => o.AddPolicy("AdminOnly", p => p.RequireRole(Roles.Admin)));

    // ---------- 三层架构 DI 注册 ----------
    builder.Services.AddSingleton<SseHub>();

    // ---------- AI 摘要（计划 8）：配置驱动 + 后台队列，无密钥自动降级规则小编 ----------
    builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai"));
    builder.Services.AddHttpClient("ai", (sp, c) =>
    {
        var opt = sp.GetRequiredService<IOptions<AiOptions>>().Value;
        c.Timeout = TimeSpan.FromSeconds(Math.Clamp(opt.TimeoutSeconds, 5, 120));
    });
    builder.Services.AddSingleton<AiTaskQueue>();
    builder.Services.AddSingleton<IAiSummaryService, OpenAiSummaryService>();
    builder.Services.AddHostedService<AiGenerationService>();

    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<NewsRepository>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<PortalService>();
    builder.Services.AddScoped<AdminNewsService>();
    builder.Services.AddScoped<TaxonomyService>();
    builder.Services.AddScoped<BannerService>();
    builder.Services.AddScoped<UploadService>();
    builder.Services.AddScoped<DashboardService>();
    builder.Services.AddScoped<StatsService>();
    builder.Services.AddScoped<LogService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "AdPulse API", Version = "v1",
            Description = "广告行业新闻门户 API（面试演示作品，数据为演示样例）"
        });
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "输入登录接口返回的 token（无需 Bearer 前缀）",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // ---------- 启动即迁移 + WAL + 种子（幂等） ----------
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        DbSeeder.Seed(db);
    }

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "AdPulse API v1"));
    }

    app.UseCors("DevCors");
    app.UseAuthentication();
    app.UseAuthorization();

    // 上传目录：开发默认 wwwroot/uploads，生产由 UploadPath 配置指向 /var/adpulse/uploads
    var uploadPath = app.Configuration["UploadPath"];
    if (string.IsNullOrWhiteSpace(uploadPath))
        uploadPath = Path.Combine(contentRoot, "wwwroot", "uploads");
    Directory.CreateDirectory(uploadPath);
    app.UseStaticFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadPath),
        RequestPath = "/uploads"
    });

    app.MapControllers();
    app.MapGet("/api/health", (AppDbContext db) => Results.Json(new
    {
        code = 0,
        message = "ok",
        data = new { status = "up", database = db.Database.CanConnect(), time = DateTime.UtcNow }
    }));

    // 未匹配的 /api 路由统一返回 40400 JSON（计划 2.3）
    app.MapFallback("/api/{**rest}", () => Results.Json(
        new { code = 40400, message = "接口不存在", data = (object?)null }, statusCode: 404));

    Log.Information("AdPulse API 启动完成，数据库：{Db}", dataSource);
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "AdPulse API 启动失败");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }