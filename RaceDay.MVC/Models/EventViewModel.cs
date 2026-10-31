namespace RaceDay.MVC.Models
{
    public class EventViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = "";

        public string EventType { get; set; } = "Run";

        public DateOnly EventDate { get; set; }

        public string Location { get; set; } = "";

        public string? Description { get; set; }

        public int OrganiserId { get; set; }

        public string? OrganisationName { get; set; }
    }
}
