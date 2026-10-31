namespace RaceDay.MVC.Models
{
    public class OrganiserEnrolmentViewModel
    {
        public int EnrolmentId { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = "";
        public string ParticipantEmail { get; set; } = "";
        public string EmergencyContact { get; set; } = "";

        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";

        public DateOnly EnrolmentDate { get; set; }
        public string Status { get; set; } = "";

        public bool HasResult { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string? ResultStatus { get; set; }
    }
}
