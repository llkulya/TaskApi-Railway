using System.ComponentModel.DataAnnotations;

namespace TaskApi.Dto.Commands
{
    public class CommentCreateCommand
    {
        [Required]
        public string Text { get; set; } = string.Empty;

        [Required]
        public int TaskItemId { get; set; }
    }
}