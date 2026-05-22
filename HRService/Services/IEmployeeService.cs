using HRService.Dto;

namespace HRService.Services
{
    public interface IEmployeeService
    {
        Task<EmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request);
    }
}