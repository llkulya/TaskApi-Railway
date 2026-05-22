namespace TaskApi.Services
{
    public interface IEmailService
    {
        Task SendEmployeeCredentialsAsync(
            string email,
            string temporaryPassword);
    }
}