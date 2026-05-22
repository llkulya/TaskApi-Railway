using Microsoft.EntityFrameworkCore;
using TaskApi.Models;
using TaskApi.Data;

namespace TaskApi.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly ApplicationDbContext _context;

        public CommentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Comment> AddAsync(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<List<Comment>> GetByTaskIdAsync(int taskId)
        {
            return await _context.Comments
                .Where(c => c.TaskItemId == taskId)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return false;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<Comment?> GetByIdAsync(int id)
        {
            return await _context.Comments.FindAsync(id);
        }
    }
}