using landing_page_backend;
using landing_page_backend.Application.Contracts;
using landing_page_backend.Application.Services;
using landing_page_backend.Data;
using landing_page_backend.Repositories;
using landing_page_backend.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log",
        rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ??
    [
        "http://127.0.0.1:5500"
    ];
// 允許來自前端的跨域請求
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var developNote = builder.Configuration["Develop_note:note"];
var swaggerDescription = BuildInfo.SwaggerDescription;
if (!string.IsNullOrWhiteSpace(developNote))
    swaggerDescription += "<br/>" + developNote.Trim();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "landing-page-backend",
        Version = "v1",
        Description = swaggerDescription
    });
});
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddDbContext<MyDbContext>(options =>
{
    var provider = builder.Configuration[$"{DatabaseOptions.SectionName}:Provider"]?.ToLowerInvariant() ?? "mssql";

    if (provider == "postgresql" || provider == "postgres")
    {
        var connectionString = builder.Configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("Connection string 'PostgreSql' is missing.");
        options.UseNpgsql(connectionString);
    }
    else if (provider == "mssql" || provider == "sqlserver")
    {
        var connectionString = builder.Configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string 'SqlServer' is missing.");
        options.UseSqlServer(connectionString);
    }
    else
    {
        throw new InvalidOperationException($"Unsupported database provider: {provider}");
    }
});
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IApplicationService<>), typeof(ApplicationService<>));
builder.Services.AddScoped<ISitePageService, SitePageService>();
builder.Services.AddScoped<IMediaFileService, MediaFileService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<MyDbContext>();

var app = builder.Build();

// 啟動時自動加入測試資料
DateSeed.SeedData(app.Services);

ApplyPathBaseFromConfiguration(app);

// Configure the HTTP request pipeline.
// IIS 上常設為 Staging／Production：僅 Development 會看不到 /swagger（易誤判為 404）
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

// 讓 /uploads 路徑可直接存取上傳的圖片
var uploadPath = Path.Combine(app.Environment.ContentRootPath,
    app.Configuration["MediaFiles:UploadPath"] ?? "uploads");
Directory.CreateDirectory(uploadPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

app.UseCors("AllowFrontend");
app.UseAuthorization();

// 根路徑未掛控制器時預設為 404；加此可確認 IIS 轉發有進 Kestrel（含子路徑時 PathBase 會反映在 PathBase）
app.MapGet("/", async (HttpContext ctx, MyDbContext db) =>
{
    var dbConnection = false;
    try
    {
        dbConnection = await db.Database.CanConnectAsync();
    }
    catch
    {
        dbConnection = false;
    }

    return Results.Json(new
    {
        service = "landing-page-backend",
        pathBase = ctx.Request.PathBase.Value,
        swagger = $"{ctx.Request.PathBase}/swagger",
        DBConnection = dbConnection
    });
});

app.MapHealthChecks("/health");

app.MapControllers();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

static void ApplyPathBaseFromConfiguration(WebApplication application)
{
    var opts = application.Configuration.GetSection(ApplicationOptions.SectionName).Get<ApplicationOptions>()
        ?? new ApplicationOptions();
    var pathBase = NormalizeApplicationPathBase(opts.PathBase);
    if (pathBase.HasValue)
        application.UsePathBase(pathBase);
}

static PathString NormalizeApplicationPathBase(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        return PathString.Empty;

    var t = raw.Trim();
    if (t == "/")
        return PathString.Empty;

    if (!t.StartsWith('/'))
        t = "/" + t;

    t = t.TrimEnd('/');
    if (t.Length == 0 || t == "/")
        return PathString.Empty;

    return new PathString(t);
}

public partial class Program { }
