using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IAuthService authService,
            ILogger<UsersController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Внутрішній endpoint для реєстрації співробітника з HR Service
        /// </summary>
        [HttpPost("register-employee")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegisterEmployeeResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RegisterEmployeeResponse>> RegisterEmployee(
            [FromBody] RegisterEmployeeRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Internal employee registration endpoint called. ServiceName: {ServiceName}, MethodName: {MethodName}, EmployeeEmail: {EmployeeEmail}",
                    "TaskService",
                    nameof(RegisterEmployee),
                    request.Email);

                var response = await _authService.RegisterEmployeeAsync(request);

                return Created(
                    $"/api/users/{response.UserId}",
                    response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    type = "ValidationError"
                });
            }
        }
    }
}