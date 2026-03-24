namespace TaskApi.Dto.Commands
{
    public class AssignExecutorCommand
    {
        public int TaskId { get; set; }
        public int ExecutorId { get; set; }
        public int Version { get; set; } // Для оптимістичного блокування
    }
}