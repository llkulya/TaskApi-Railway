using HRService.Attributes;
using HRService.Dto;

namespace HRService.Services
{
    public interface ITaskAccountClient
    {
        [Retry(maxAttempts: 3, delayMilliseconds: 1000)]
        Task<RegisterEmployeeTaskResponse> RegisterEmployeeAsync(
            RegisterEmployeeRequest request);
    }
}