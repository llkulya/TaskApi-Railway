using HRService.Dto;
using HRService.Models;

namespace HRService.Services
{
    public class EmployeeService : IEmployeeService
    {
        private static readonly List<Employee> Employees = new();
        private static int _nextId = 1;

        private readonly ITaskAccountClient _taskAccountClient;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            ITaskAccountClient taskAccountClient,
            ILogger<EmployeeService> logger)
        {
            _taskAccountClient = taskAccountClient;
            _logger = logger;
        }

        public async Task<EmployeeResponse> CreateEmployeeAsync(
            CreateEmployeeRequest request)
        {
            var employee = new Employee
            {
                Id = _nextId++,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Position = request.Position,
                Department = request.Department,
                CreatedAt = DateTime.UtcNow
            };

            Employees.Add(employee);

            _logger.LogInformation(
                "HRService: employee created locally. ServiceName={ServiceName}, MethodName={MethodName}, EmployeeEmail={EmployeeEmail}, EmployeeId={EmployeeId}",
                "HRService",
                nameof(CreateEmployeeAsync),
                employee.Email,
                employee.Id);

            var taskRequest = new RegisterEmployeeRequest
            {
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                Position = employee.Position,
                Department = employee.Department
            };

            try
            {
                var taskResponse =
                    await _taskAccountClient.RegisterEmployeeAsync(taskRequest);

                _logger.LogInformation(
                    "HRService: TaskApi account created successfully. ServiceName={ServiceName}, MethodName={MethodName}, EmployeeEmail={EmployeeEmail}, TaskUserId={TaskUserId}, Success={Success}",
                    "HRService",
                    nameof(CreateEmployeeAsync),
                    employee.Email,
                    taskResponse.UserId,
                    true);

                return new EmployeeResponse
                {
                    EmployeeId = employee.Id,
                    Email = employee.Email,
                    TaskAccountCreated = true,
                    TaskUserId = taskResponse.UserId,
                    Message = "Employee created in HRService and user account created in TaskApi."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "HRService: TaskApi account creation failed after retry attempts. ServiceName={ServiceName}, MethodName={MethodName}, EmployeeEmail={EmployeeEmail}, Success={Success}",
                    "HRService",
                    nameof(CreateEmployeeAsync),
                    employee.Email,
                    false);

                return new EmployeeResponse
                {
                    EmployeeId = employee.Id,
                    Email = employee.Email,
                    TaskAccountCreated = false,
                    TaskUserId = null,
                    Message = "Employee created in HRService, but TaskApi account was not created after retry attempts."
                };
            }
        }
    }
}