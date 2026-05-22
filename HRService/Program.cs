using HRService.Services;
using HRService.Interceptors;
using Serilog;
using Serilog.Events;

var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL")
             ?? "http://localhost:5341";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithProcessId()
    .Enrich.WithProperty("ServiceName", "HRService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq(
        serverUrl: seqUrl,
        restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var port = Environment.GetEnvironmentVariable("PORT");

    if (!string.IsNullOrWhiteSpace(port))
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    var enableSwagger =
        Environment.GetEnvironmentVariable("ENABLE_SWAGGER") == "true";

    builder.Host.UseSerilog(Log.Logger, dispose: true);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var taskServiceUrl =
        Environment.GetEnvironmentVariable("TASK_SERVICE_URL")
        ?? builder.Configuration["TaskService:BaseUrl"]
        ?? "http://localhost:8081";

    // Якщо RetryInterceptor вже доданий через DI — залишаємо його
    builder.Services.AddSingleton<RetryInterceptor>();

    builder.Services.AddScoped<IEmployeeService, EmployeeService>();

    builder.Services.AddHttpClient<ITaskAccountClient, TaskAccountClient>(client =>
    {
        client.BaseAddress = new Uri(taskServiceUrl);
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment() || enableSwagger)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapGet("/test-seq", () =>
    {
        Log.Information(
            "HRService test log sent to Seq. ServiceName: {ServiceName}, MethodName: {MethodName}, Success: {Success}",
            "HRService",
            "TestSeq",
            true);

        return "HRService log sent to Seq";
    });

    app.UseAuthorization();

    app.MapControllers();

    Log.Information(
        "HRService started. ServiceName: {ServiceName}, SeqUrl: {SeqUrl}, TaskServiceUrl: {TaskServiceUrl}",
        "HRService",
        seqUrl,
        taskServiceUrl);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "HRService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}