using TaskApi.Repositories;
using TaskApi.Services;

var builder = WebApplication.CreateBuilder(args);

// --- РЕЄСТРАЦІЯ СЕРВІСІВ (Dependency Injection) ---

// Реєструємо репозиторій та сервіс
builder.Services.AddScoped<ITaskRepository, MockTaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();

// Додає підтримку контролерів
builder.Services.AddControllers();

// Налаштування Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- КОНФІГУРАЦІЯ КОНВЕЄРА (Middleware) ---

// Swagger працює тільки в режимі розробки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Маршрутизація: визначає, який контролер викликати
app.UseRouting();

// Авторизація (поки що просто стандартна заглушка)
app.UseAuthorization();

// Мапимо контролери на адреси
app.MapControllers();

app.Run();