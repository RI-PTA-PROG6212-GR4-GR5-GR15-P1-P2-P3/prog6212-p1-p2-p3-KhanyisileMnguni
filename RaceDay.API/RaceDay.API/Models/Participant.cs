namespace RaceDay.API.Models
{
    public class Participant
    {
        public int ParticipantID { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string EmergencyContact { get; set; } = string.Empty;

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
