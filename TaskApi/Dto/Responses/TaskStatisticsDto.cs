namespace TaskApi.Dto.Responses
{
    public class TaskStatisticsDto
    {
        public int TotalTasks { get; set; }

        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int DoneTasks { get; set; }

        public int OverdueTasks { get; set; }

        public double CompletionRate { get; set; }
        public double AverageCompletionTime { get; set; }
    }
}