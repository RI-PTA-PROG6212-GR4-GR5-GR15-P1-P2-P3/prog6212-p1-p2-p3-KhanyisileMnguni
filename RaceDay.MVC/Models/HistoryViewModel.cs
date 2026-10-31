namespace RaceDay.MVC.Models
{
    public class HistoryViewModel
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = "";
        public DateOnly EventDate { get; set; }
        public string Location { get; set; } = "";

        public string CategoryName { get; set; } = "";
        public decimal DistanceKm { get; set; }

        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string Status { get; set; } = "";

        public int CategoryId { get; set; }
        public int TotalFinishers { get; set; }
    }
}