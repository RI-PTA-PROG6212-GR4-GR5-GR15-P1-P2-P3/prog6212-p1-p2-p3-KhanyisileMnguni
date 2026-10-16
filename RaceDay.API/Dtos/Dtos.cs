using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos;

// ---------- Requests ----------

public class RegisterRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = null!;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = null!;
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = null!;
    /// <summary>"Organiser" or "Participant".</summary>
    [Required] public string Role { get; set; } = null!;
    /// <summary>Organisers only (optional).</summary>
    [StringLength(150)] public string? OrganisationName { get; set; }
    /// <summary>Participants only (required for Participants; the Part 1 table has it as NOT NULL).</summary>
    public DateOnly? DateOfBirth { get; set; }
    /// <summary>Participants only (optional).</summary>
    [StringLength(100)] public string? EmergencyContact { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = null!;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = null!;
    [StringLength(100)] public string? EmergencyContact { get; set; }
    [StringLength(150)] public string? OrganisationName { get; set; }
}

public class EventRequest
{
    [Required, StringLength(100)] public string EventName { get; set; } = null!;
    /// <summary>Run, Walk or Cycle.</summary>
    [Required, RegularExpression("(?i)^(run|walk|cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = null!;
    [Required] public DateOnly? EventDate { get; set; }
    [Required, StringLength(150)] public string Location { get; set; } = null!;
    [StringLength(1000)] public string? Description { get; set; }
}

public class CategoryRequest
{
    [Required, StringLength(50)] public string CategoryName { get; set; } = null!;
    [Range(0.01, 999.99)] public decimal DistanceKM { get; set; }
    [Range(1, 100000)] public int MaxParticipants { get; set; } = 100;
    [Range(0, 999999.99)] public decimal EntryFee { get; set; }
}

/// <summary>Optional body. The server always stamps today's date as the enrolment date.</summary>
public class EnrolRequest
{
    public DateOnly? EnrolmentDate { get; set; }
}

public class ResultRequest
{
    /// <summary>hh:mm:ss, e.g. "00:52:30". Required when Status is Finished.</summary>
    public TimeSpan? FinishTime { get; set; }
    [Range(1, 100000)] public int? Position { get; set; }
    /// <summary>Finished, DNF, DNS or Disqualified.</summary>
    [Required, RegularExpression("(?i)^(finished|dnf|dns|disqualified)$",
        ErrorMessage = "Status must be Finished, DNF, DNS or Disqualified.")]
    public string Status { get; set; } = "Finished";
}

// ---------- Responses ----------

public record AuthResponse(int UserId, string Role, string Name);

public record LoginResponse(int UserId, string Role, string Name, string Token, DateTime ExpiresAtUtc);

public record ProfileResponse(int UserId, string Role, string Name, string Email,
    string? OrganisationName, DateOnly? DateOfBirth, string? EmergencyContact);

public record EventResponse(int EventId, string EventName, string EventType, DateOnly EventDate,
    string Location, string? Description, string OrganiserName, string? OrganisationName);

public record CategoryResponse(int CategoryId, int EventId, string CategoryName, decimal DistanceKm,
    int MaxParticipants, decimal EntryFee, int EnrolledCount);

public record EventDetailResponse(int EventId, string EventName, string EventType, DateOnly EventDate,
    string Location, string? Description, string OrganiserName, string? OrganisationName,
    IReadOnlyList<CategoryResponse> Categories);

public record EnrolmentResponse(int EnrolmentId, DateOnly EnrolmentDate, string Status,
    int CategoryId, string CategoryName, decimal DistanceKm, decimal EntryFee,
    int EventId, string EventName, DateOnly EventDate, string Location);

public record OrganiserEnrolmentResponse(int EnrolmentId, int ParticipantId, string ParticipantName,
    string ParticipantEmail, string? EmergencyContact, int CategoryId, string CategoryName,
    DateOnly EnrolmentDate, string Status);

public record ResultResponse(int ResultId, int EnrolmentId, TimeSpan? FinishTime, int? Position, string Status);

public record HistoryItemResponse(int ResultId, int EnrolmentId, int EventId, string EventName,
    DateOnly EventDate, string Location, string CategoryName, decimal DistanceKm,
    TimeSpan? FinishTime, int? Position, string Status);