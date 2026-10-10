namespace RaceDay.API.Models
{
    public class Event
    {
        public int EventID { get; set; }

        public int OrganiserID { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Organiser Organiser { get; set; } = null!;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}