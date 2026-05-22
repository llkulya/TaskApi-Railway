using HRService.Dto;
using System.Net;
using System.Net.Http.Json;

namespace HRService.Services
{
    public class TaskAccountClient : ITaskAccountClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TaskAccountClient> _logger;

        public TaskAccountClient(
            HttpClient httpClient,
            ILogger<TaskAccountClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<RegisterEmployeeTaskResponse> RegisterEmployeeAsync(
            RegisterEmployeeRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/users/register-employee",
                request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _logger.LogWarning(
                    "TaskApi returned unsuccessful response. ServiceName={ServiceName}, MethodName={MethodName}, EmployeeEmail={EmployeeEmail}, StatusCode={StatusCode}, Error={Error}",
                    "HRService",
                    nameof(RegisterEmployeeAsync),
                    request.Email,
                    response.StatusCode,
                    errorText);

                if (ShouldRetryStatusCode(response.StatusCode))
                {
                    throw new HttpRequestException(
                        $"TaskApi temporary error. StatusCode={response.StatusCode}. Error={errorText}",
                        null,
                        response.StatusCode);
                }

                throw new InvalidOperationException(
                    $"TaskApi rejected request. StatusCode={response.StatusCode}. Error={errorText}");
            }

            var result =
                await response.Content.ReadFromJsonAsync<RegisterEmployeeTaskResponse>();

            if (result == null)
                throw new InvalidOperationException("TaskApi returned empty response.");

            return result;
        }

        private static bool ShouldRetryStatusCode(HttpStatusCode statusCode)
        {
            var code = (int)statusCode;

            return code == 408 ||
                   code == 429 ||
                   code >= 500;
        }
    }
}