namespace TaskApi.Services
{
    public class DevelopmentEmailService : IEmailService
    {
        private readonly ILogger<DevelopmentEmailService> _logger;

        public DevelopmentEmailService(
            ILogger<DevelopmentEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmployeeCredentialsAsync(
            string email,
            string temporaryPassword)
        {
            _logger.LogInformation(
                "Development email mode. Employee credentials were not sent. Email={Email}, TemporaryPassword={TemporaryPassword}",
                email,
                temporaryPassword);

            return Task.CompletedTask;
        }
    }
}