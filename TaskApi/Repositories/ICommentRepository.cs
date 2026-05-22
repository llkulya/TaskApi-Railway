using TaskApi.Models;

namespace TaskApi.Repositories
{
    public interface ICommentRepository
    {
        Task<Comment> AddAsync(Comment comment);
        Task<List<Comment>> GetByTaskIdAsync(int taskId);
        Task<bool> DeleteAsync(int id);
        Task<Comment?> GetByIdAsync(int id);
    }
}