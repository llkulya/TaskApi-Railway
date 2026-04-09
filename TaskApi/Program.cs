using Microsoft.EntityFrameworkCore;
using TaskApi.Data; 
using TaskApi.Repositories;
using TaskApi.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IExecutorRepository, ExecutorRepository>();
builder.Services.AddScoped<IProjectManagerRepository, ProjectManagerRepository>();

builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IExecutorService, ExecutorService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();