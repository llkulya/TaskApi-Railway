using Microsoft.AspNetCore.Mvc;
using TaskApi.Dto.Commands;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CommentCreateCommand command)
        {
            var result = await _commentService.AddAsync(command);
            return Ok(result);
        }

        [HttpGet("task/{taskId}")]
        public async Task<IActionResult> GetByTask(int taskId)
        {
            var result = await _commentService.GetByTaskIdAsync(taskId);
            return Ok(result);
        }
    }
}