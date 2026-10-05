// @author: Sebastián Alvarado García C5C341
// @author: Justin Andrés Badilla Ramírez C4C928
// @author: Abigail Crystal García Bonilla C5F263
// @author: Frank de Jesús Villalobos Elizondo C5K944

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