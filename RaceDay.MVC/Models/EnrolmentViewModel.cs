namespace RaceDay.MVC.Models
{
    public class EnrolmentViewModel
    {
        public int EnrolmentId { get; set; }
        public DateOnly EnrolmentDate { get; set; }
        public string Status { get; set; } = "";

        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }

        public int EventId { get; set; }
        public string EventName { get; set; } = "";
        public DateOnly EventDate { get; set; }
        public string Location { get; set; } = "";
    }
}