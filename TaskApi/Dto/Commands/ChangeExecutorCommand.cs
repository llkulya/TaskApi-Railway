namespace TaskApi.Dto.Commands
{
    public class ChangeExecutorCommand
    {
        public int TaskId { get; set; }
        public int NewExecutorId { get; set; }
        public string? Reason { get; set; } // Опціональна причина зміни
        public int Version { get; set; } // Для оптимістичного блокування
    }
}