namespace TaskApi.Services
{
    /// <summary>
    /// Фоновий сервіс для очищення старих файлів логів
    /// </summary>
    public sealed class LogCleanService : IHostedService, IAsyncDisposable
    {
        private readonly ILogger<LogCleanService> _logger;
        private readonly Task _completedTask = Task.CompletedTask;
        private Timer? _timer;

        // Зберігати логи за останні 30 днів
        private const int RetentionDays = 30;

        // Запускати перевірку кожні 5 годин
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(5);

        public LogCleanService(ILogger<LogCleanService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "{Service} is running. Will clean logs older than {Days} days every {Hours} hours.",
                nameof(LogCleanService),
                RetentionDays,
                _checkInterval.TotalHours);

            // Запускаємо одразу і потім кожні 5 годин
            _timer = new Timer(DoWork, null, TimeSpan.Zero, _checkInterval);

            return _completedTask;
        }

        private void DoWork(object? state)
        {
            var logDirectory = Environment.GetEnvironmentVariable("LOG_DIRECTORY")
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            if (!Directory.Exists(logDirectory))
            {
                _logger.LogWarning(
                    "Log directory {Directory} does not exist",
                    logDirectory);
                return;
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);
            var deletedCount = 0;
            var errorCount = 0;

            foreach (var file in Directory.GetFiles(logDirectory, "app-*.log"))
            {
                try
                {
                    if (File.GetCreationTimeUtc(file) < cutoffDate)
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogInformation(
                            "Deleted old log file {FileName}",
                            Path.GetFileName(file));
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex,
                        "Failed to delete log file {FileName}",
                        Path.GetFileName(file));
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation(
                    "Log cleanup completed. Deleted {DeletedCount} files.",
                    deletedCount);
            }

            if (errorCount > 0)
            {
                _logger.LogWarning(
                    "Log cleanup completed with {ErrorCount} errors.",
                    errorCount);
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "{Service} is stopping.",
                nameof(LogCleanService));

            _timer?.Change(Timeout.Infinite, 0);

            return _completedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_timer is IAsyncDisposable timer)
            {
                await timer.DisposeAsync();
            }

            _timer = null;
        }
    }
}