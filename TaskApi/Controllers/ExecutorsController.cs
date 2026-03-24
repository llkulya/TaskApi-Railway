using Microsoft.AspNetCore.Mvc;
using TaskApi.Dto.Commands;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExecutorsController : ControllerBase
    {
        private readonly IExecutorService _executorService;

        public ExecutorsController(IExecutorService executorService)
        {
            _executorService = executorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var executors = await _executorService.GetAllAsync();
            return Ok(executors);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ExecutorCreateCommand command)
        {
            var executor = await _executorService.CreateAsync(command);
            return Ok(executor);
        }
    }
}