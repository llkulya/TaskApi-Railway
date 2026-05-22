using HRService.Dto;
using HRService.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRService.Controllers
{
    [ApiController]
    [Route("api/employees")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EmployeeResponse>> CreateEmployee(
            [FromBody] CreateEmployeeRequest request)
        {
            var result = await _employeeService.CreateEmployeeAsync(request);

            return Created($"/api/employees/{result.EmployeeId}", result);
        }
    }
}