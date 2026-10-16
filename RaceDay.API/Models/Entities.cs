namespace RaceDay.API.Models;

public class Organiser
{
    public int OrganiserID { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;   // stores the HASH, never the original
    public string? OrganisationName { get; set; }
    public List<Event> Events { get; set; } = new();
}

public class Participant
{
    public int ParticipantID { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;   // stores the HASH, never the original
    public DateOnly DateOfBirth { get; set; }
    public string? EmergencyContact { get; set; }
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = null!;
    /// <summary>Run, Walk or Cycle (required by the Part 2 brief).</summary>
    public string EventType { get; set; } = "Run";
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = null!;
    public string? Description { get; set; }
    public Organiser Organiser { get; set; } = null!;
    public List<Category> Categories { get; set; } = new();
}

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = null!;
    public decimal DistanceKM { get; set; }
    public int MaxParticipants { get; set; } = 100;
    public decimal EntryFee { get; set; }
    public Event Event { get; set; } = null!;
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Enrolment
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int CategoryID { get; set; }
    public DateOnly EnrolmentDate { get; set; }
    public string Status { get; set; } = "Registered";
    public Participant Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
    public Enrolment Enrolment { get; set; } = null!;
}