using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;
using TaskApi.Models;

namespace TaskApi.Services
{
    public interface ICommentService
    {
        Task<CommentDto> AddAsync(CommentCreateCommand command);
        Task<List<CommentDto>> GetByTaskIdAsync(int taskId);
        Task<bool> DeleteAsync(int id);
    }
}