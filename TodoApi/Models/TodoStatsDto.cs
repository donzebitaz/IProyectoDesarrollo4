namespace TodoApi.Models
{
    public class TodoStatsDto
    {
        public int TotalTasks { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Canceled { get; set; }
        public int OverdueTasks { get; set; }
        public double? AverageDaysToComplete { get; set; }
    }
}