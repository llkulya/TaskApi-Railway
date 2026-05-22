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

// ===== НАЛАШТУВАННЯ SERILOG =====
var logDirectory = Environment.GetEnvironmentVariable("LOG_DIRECTORY")
                   ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

if (!Directory.Exists(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
    Console.WriteLine($"✅ Створено директорію для логів: {logDirectory}");
}

// 1. Отримуємо URL для Seq з середовища або використовуємо дефолт
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL")
             ?? "http://localhost:5341";

#if DEBUG
var consoleRestrictedLevel = LogEventLevel.Debug;
#else
var consoleRestrictedLevel = LogEventLevel.Warning;
#endif

Log.Logger = new LoggerConfiguration()
    // Рівні логування
#if DEBUG
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
#else
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
#endif
    // Збагачення контекстною інформацією
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithProcessId()
    .Enrich.WithProperty("ApplicationName", "ProjectManagementAPI")

    // Sink 1: Консоль
    .WriteTo.Console(
    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
    restrictedToMinimumLevel: consoleRestrictedLevel)

    // Sink 2: Файл (асинхронний, JSON формат)
    .WriteTo.Async(a => a.File(
        new CompactJsonFormatter(),
        path: Path.Combine(logDirectory, "app-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 52428800, // 50MB
        encoding: System.Text.Encoding.UTF8,
        restrictedToMinimumLevel: LogEventLevel.Information))

    // Sink 3: Audit-файл для важливих подій безпеки та аудиту
    .WriteTo.Async(a => a.File(
        new CompactJsonFormatter(),
        path: Path.Combine(logDirectory, "audit-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90,
        fileSizeLimitBytes: 52428800, // 50MB
        encoding: System.Text.Encoding.UTF8,
        restrictedToMinimumLevel: LogEventLevel.Warning))

    // Sink 4: Seq (оновлено для підтримки env)
    .WriteTo.Seq(
        serverUrl: seqUrl,
        restrictedToMinimumLevel: LogEventLevel.Information)

    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var enableSwagger =
    Environment.GetEnvironmentVariable("ENABLE_SWAGGER") == "true";

// Підключення Serilog до хосту
builder.Host.UseSerilog(Log.Logger, dispose: true);

// 2. Оновлення Connection String для підтримки Docker (env)
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

// ===== JWT НАЛАШТУВАННЯ =====
var jwtSettings = new JwtSettings
{
    Secret = Environment.GetEnvironmentVariable("JWT_SECRET")
             ?? builder.Configuration["JwtSettings:Secret"]
             ?? throw new InvalidOperationException("JWT Secret is not configured"),
    Issuer = builder.Configuration["JwtSettings:Issuer"] ?? "TaskApi",
    Audience = builder.Configuration["JwtSettings:Audience"] ?? "TaskApiUsers",
    TokenExpiryMinutes = int.TryParse(
        builder.Configuration["JwtSettings:TokenExpiryMinutes"], out var minutes)
        ? minutes : 60,
    RefreshTokenExpiryDays = int.TryParse(
        builder.Configuration["JwtSettings:RefreshTokenExpiryDays"], out var days)
        ? days : 7
};

builder.Services.AddSingleton(jwtSettings);

// ===== АУТЕНТИФІКАЦІЯ JWT + GOOGLE SSO =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = "Cookies";
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
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };
})
.AddGoogle(options =>
{
    // 3. Оновлення Google Auth для підтримки Docker (env)
    options.ClientId =
        Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
        ?? builder.Configuration["GoogleAuth:ClientId"]
        ?? throw new InvalidOperationException("Google ClientId is not configured");

    options.ClientSecret =
        Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
        ?? builder.Configuration["GoogleAuth:ClientSecret"]
        ?? throw new InvalidOperationException("Google ClientSecret is not configured");

    options.CallbackPath = "/signin-google";
    options.SignInScheme = "Cookies";
});

// ===== СЕСІЇ =====
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== РЕПОЗИТОРІЇ =====
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IExecutorRepository, ExecutorRepository>();
builder.Services.AddScoped<IProjectManagerRepository, ProjectManagerRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// ===== СЕРВІСИ =====
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IExecutorService, ExecutorService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IProjectManagerService, ProjectManagerService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISsoService, SsoService>();
var resendApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");

if (!string.IsNullOrWhiteSpace(resendApiKey))
{
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, DevelopmentEmailService>();
}

// ===== AOP / INTERCEPTORS =====
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

    var interceptors = new IInterceptor[]
    {
        provider.GetRequiredService<ExceptionHandlingInterceptor>(),
        provider.GetRequiredService<ValidationInterceptor>(),
        provider.GetRequiredService<PerformanceInterceptor>(),
        provider.GetRequiredService<MethodLoggingInterceptor>(),
        provider.GetRequiredService<CachingInterceptor>()
    };

    return proxyGenerator.CreateInterfaceProxyWithTarget(
        inner,
        interceptors);
});

// IHttpContextAccessor — для отримання IP у сервісах
builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache();

// LogCleanService — фоновий сервіс очищення старих логів
builder.Services.AddHostedService<LogCleanService>();

// ===== SWAGGER =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskApi", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Введіть: Bearer {токен}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===== БАЗА ДАНИХ =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

var applyMigrations =
    Environment.GetEnvironmentVariable("APPLY_MIGRATIONS") == "true";

if (applyMigrations)
{
    try
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        dbContext.Database.Migrate();

        app.Logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error while applying database migrations.");
        throw;
    }
}

// ===== МІГРАЦІЯ ТА SEED =====
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    SeedData.Initialize(dbContext);
}

// ===== MIDDLEWARE =====
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<PerformanceLoggingMiddleware>();
app.UseMiddleware<MetricsMiddleware>();

// Correlation ID middleware — додає контекст до всіх логів запиту
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();

    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("RequestPath", context.Request.Path))
    using (LogContext.PushProperty("RequestMethod", context.Request.Method))
    using (LogContext.PushProperty("IPAddress", context.Connection.RemoteIpAddress?.ToString()))
    using (LogContext.PushProperty("UserAgent", context.Request.Headers["User-Agent"].ToString()))
    {
        await next();
    }
});

// Тестовий ендпоінт для перевірки Seq (тепер використовує актуальний URL)
app.MapGet("/test-seq", () =>
{
    Log.Information(" Тестовий лог: система логування в Seq працює!");
    Log.Warning(" Попередження для тесту");
    Log.Error(new Exception("Тестова помилка"), " Помилка для тесту");
    return $"Логи надіслані до Seq! Перевірте: {seqUrl}";
});

if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseSession();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                 ?? context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

    if (!string.IsNullOrEmpty(userId))
    {
        using (LogContext.PushProperty("UserId", userId))
        {
            await next();
        }
    }
    else
    {
        await next();
    }
});

app.UseAuthorization();
app.MapControllers();

// Логування запуску
Log.Information("Application starting in {Environment} environment",
    app.Environment.EnvironmentName);

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