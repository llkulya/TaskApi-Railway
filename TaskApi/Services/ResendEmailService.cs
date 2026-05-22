using Resend;

namespace TaskApi.Services
{
    public class ResendEmailService : IEmailService
    {
        private readonly ILogger<ResendEmailService> _logger;
        private readonly IResend _resendClient;

        public ResendEmailService(ILogger<ResendEmailService> logger)
        {
            _logger = logger;

            var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY")
                ?? throw new InvalidOperationException("RESEND_API_KEY не вказано.");

            _resendClient = ResendClient.Create(apiKey);
        }

        public async Task SendEmployeeCredentialsAsync(
            string email,
            string temporaryPassword)
        {
            var targetEmail =
                Environment.GetEnvironmentVariable("TEST_EMAIL") ?? email;

            var response = await _resendClient.EmailSendAsync(new EmailMessage
            {
                From = "onboarding@resend.dev",
                To = new[] { targetEmail },
                Subject = "Дані для входу в TaskApi",
                HtmlBody =
                    $"""
                    <p>Для вас створено обліковий запис у TaskApi.</p>
                    <p><b>Email:</b> {email}</p>
                    <p><b>Тимчасовий пароль:</b> {temporaryPassword}</p>
                    <p>Після першого входу рекомендується змінити пароль.</p>
                    """
            });

            if (!response.Success)
            {
                _logger.LogError(
                    "Resend failed to send employee credentials. EmployeeEmail={EmployeeEmail}, TargetEmail={TargetEmail}, Error={Error}",
                    email,
                    targetEmail,
                    response.Content);

                throw new InvalidOperationException(
                    "Не вдалося надіслати дані для входу через Resend.");
            }

            _logger.LogInformation(
                "Employee credentials sent through Resend. EmployeeEmail={EmployeeEmail}, TargetEmail={TargetEmail}",
                email,
                targetEmail);
        }
    }
}