using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Castle.DynamicProxy;
using TaskApi.Interceptors;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text;
using TaskApi;
using TaskApi.Configuration;
using TaskApi.Data;
using TaskApi.Middleware;
using TaskApi.Repositories;
using TaskApi.Services;

// ===== ДОПОМІЖНІ МЕТОДИ ДЛЯ RAILWAY / DOCKER =====
string? GetEnv(params string[] names)
{
    foreach (var name in names)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value))
            return value;
    }
    return null;
}

string? BuildConnectionStringFromMysqlUrl(string? mysqlUrl)
{
    if (string.IsNullOrWhiteSpace(mysqlUrl))
        return null;

    try
    {
        var uri = new Uri(mysqlUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1
            ? Uri.UnescapeDataString(userInfo[1])
            : string.Empty;

        var database = uri.AbsolutePath.TrimStart('/');

        return $"Server={uri.Host};" +
               $"Port={uri.Port};" +
               $"Database={database};" +
               $"User={user};" +
               $"Password={password};" +
               $"CharSet=utf8mb4;";
    }
    catch { return null; }
}

// ===== НАЛАШТУВАННЯ SERILOG =====
var logDirectory = Environment.GetEnvironmentVariable("LOG_DIRECTORY")
                   ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

if (!Directory.Exists(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
}

var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";

#if DEBUG
var consoleRestrictedLevel = LogEventLevel.Debug;
#else
var consoleRestrictedLevel = LogEventLevel.Warning;
#endif

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("ApplicationName", "ProjectManagementAPI")
    .WriteTo.Console(restrictedToMinimumLevel: consoleRestrictedLevel)
    .WriteTo.Seq(serverUrl: seqUrl, restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Налаштування порту для Railway
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Host.UseSerilog(Log.Logger, dispose: true);

// ===== ФОРМУВАННЯ CONNECTION STRING =====
var connectionString =
    BuildConnectionStringFromMysqlUrl(GetEnv("MYSQL_URL", "MYSQLURL"))
    ?? GetEnv("DATABASE_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string not found.");
}

// ===== JWT НАЛАШТУВАННЯ =====
var jwtSettings = new JwtSettings
{
    Secret = Environment.GetEnvironmentVariable("JWT_SECRET")
             ?? builder.Configuration["JwtSettings:Secret"]
             ?? throw new InvalidOperationException("JWT Secret is not configured"),
    Issuer = builder.Configuration["JwtSettings:Issuer"] ?? "TaskApi",
    Audience = builder.Configuration["JwtSettings:Audience"] ?? "TaskApiUsers"
};
builder.Services.AddSingleton(jwtSettings);

// ===== АУТЕНТИФІКАЦІЯ =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie("Cookies")
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };
})
.AddGoogle(options =>
{
    options.ClientId = GetEnv("GOOGLE_CLIENT_ID") ?? builder.Configuration["GoogleAuth:ClientId"] ?? "dummy";
    options.ClientSecret = GetEnv("GOOGLE_CLIENT_SECRET") ?? builder.Configuration["GoogleAuth:ClientSecret"] ?? "dummy";
    options.CallbackPath = "/signin-google";
    options.SignInScheme = "Cookies";
});

// ===== РЕПОЗИТОРІЇ ТА СЕРВІСИ =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IExecutorRepository, ExecutorRepository>();
builder.Services.AddScoped<IProjectManagerRepository, ProjectManagerRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IExecutorService, ExecutorService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IProjectManagerService, ProjectManagerService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISsoService, SsoService>();

var resendApiKey = GetEnv("RESEND_API_KEY");
if (!string.IsNullOrWhiteSpace(resendApiKey))
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
else
    builder.Services.AddScoped<IEmailService, DevelopmentEmailService>();

// ===== INTERCEPTORS =====
builder.Services.AddSingleton<IProxyGenerator, ProxyGenerator>();
builder.Services.AddSingleton<ValidationInterceptor>();
builder.Services.AddSingleton<PerformanceInterceptor>();
builder.Services.AddSingleton<MethodLoggingInterceptor>();
builder.Services.AddSingleton<CachingInterceptor>();
builder.Services.AddSingleton<ExceptionHandlingInterceptor>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();

builder.Services.Decorate<ITaskService>((inner, provider) =>
{
    var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
    var interceptors = new IInterceptor[] {
        provider.GetRequiredService<ExceptionHandlingInterceptor>(),
        provider.GetRequiredService<ValidationInterceptor>(),
        provider.GetRequiredService<PerformanceInterceptor>(),
        provider.GetRequiredService<MethodLoggingInterceptor>(),
        provider.GetRequiredService<CachingInterceptor>()
    };
    return proxyGenerator.CreateInterfaceProxyWithTarget(inner, interceptors);
});

// ===== SWAGGER =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskApi", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new List<string>()
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===== БАЗА ДАНИХ (БЕЗ AUTO-DETECT) =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

var app = builder.Build();

// ===== МІГРАЦІЇ =====
var applyMigrations = GetEnv("APPLY_MIGRATIONS") == "true";
if (applyMigrations || app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    SeedData.Initialize(dbContext);
}

// ===== MIDDLEWARE =====
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<PerformanceLoggingMiddleware>();
app.UseMiddleware<MetricsMiddleware>();

if (app.Environment.IsDevelopment() || GetEnv("ENABLE_SWAGGER") == "true")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("Application starting...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}