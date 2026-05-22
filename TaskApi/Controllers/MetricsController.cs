using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApi.Dto.Responses;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsService _metricsService;

        public MetricsController(IMetricsService metricsService)
        {
            _metricsService = metricsService;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MetricsDto), StatusCodes.Status200OK)]
        public IActionResult GetMetrics()
        {
            var metrics = _metricsService.GetMetrics();
            return Ok(metrics);
        }
    }
}