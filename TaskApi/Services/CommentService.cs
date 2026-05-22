using System.ComponentModel.DataAnnotations;
using TaskApi.Dto.Commands;
using TaskApi.Dto.Responses;
using TaskApi.Models;
using TaskApi.Repositories;

namespace TaskApi.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        public CommentService(ICommentRepository commentRepository)
        {
            _commentRepository = commentRepository;
        }

        public async Task<CommentDto> AddAsync(CommentCreateCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Text))
            {
                throw new ValidationException("Текст коментаря не може бути порожнім");
            }

            var comment = new Comment
            {
                Text = command.Text,
                TaskItemId = command.TaskItemId,
                CreatedDate = DateTime.UtcNow
            };

            var created = await _commentRepository.AddAsync(comment);

            return new CommentDto
            {
                Id = created.Id,
                Text = created.Text,
                CreatedDate = created.CreatedDate,
                TaskItemId = created.TaskItemId
            };
        }

        public async Task<List<CommentDto>> GetByTaskIdAsync(int taskId)
        {
            var comments = await _commentRepository.GetByTaskIdAsync(taskId);
            return comments.Select(c => new CommentDto
            {
                Id = c.Id,
                Text = c.Text,
                CreatedDate = c.CreatedDate
            }).ToList();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _commentRepository.DeleteAsync(id);
        }
    }
}