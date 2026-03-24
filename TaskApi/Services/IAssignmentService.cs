using TaskApi.Dto.Commands;

namespace TaskApi.Services
{
    public interface IAssignmentService
    {
        Task<bool> AssignTaskToExecutorAsync(AssignExecutorCommand command);
        Task<bool> ChangeExecutorAsync(ChangeExecutorCommand command);
    }
}