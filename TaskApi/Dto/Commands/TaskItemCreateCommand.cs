using System.ComponentModel.DataAnnotations;

namespace TaskApi.Dto.Commands
{
    public class TaskItemCreateCommand
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "Pending";
        public string Priority { get; set; } = "Medium";
        public int? ProjectId { get; set; }
        public int? ExecutorId { get; set; }
    }
}