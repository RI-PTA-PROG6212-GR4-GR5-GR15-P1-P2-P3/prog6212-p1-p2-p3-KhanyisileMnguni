using System.ComponentModel.DataAnnotations;
using RaceDay.MVC.Models.Api;

namespace RaceDay.MVC.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your name."), StringLength(100)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That doesn't look like an email address."), StringLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please choose a password."), StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please repeat your password."), Compare(nameof(Password), ErrorMessage = "The two passwords do not match."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Please choose a role."), RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Organiser or Participant.")]
    public string Role { get; set; } = "Participant";

    [StringLength(150)]
    public string? OrganisationName { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [StringLength(100)]
    public string? EmergencyContact { get; set; }
}

/// <summary>The filters on the events home page. All of them are optional.</summary>
public class EventFilter
{
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public string? Location { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(EventType) ||
        !string.IsNullOrWhiteSpace(Location) || FromDate is not null || ToDate is not null;
}

public class HomeViewModel
{
    public EventFilter Filter { get; set; } = new();
    public List<EventResponse> Events { get; set; } = new();
    public string? Error { get; set; }
}

public class EventDetailsViewModel
{
    public EventDetailResponse Event { get; set; } = null!;
    /// <summary>Categories the logged-in Participant has already entered.</summary>
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
}

public class MyEventsViewModel
{
    public List<EnrolmentResponse> Upcoming { get; set; } = new();
    public List<EnrolmentResponse> Past { get; set; } = new();
    public string? Error { get; set; }
}

public class MyResultsViewModel
{
    public List<HistoryItemResponse> Results { get; set; } = new();
    public string? Error { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your name."), StringLength(100)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That doesn't look like an email address."), StringLength(150)]
    public string Email { get; set; } = "";

    [StringLength(100, ErrorMessage = "The emergency contact is too long.")]
    public string? EmergencyContact { get; set; }

    // Shown on the page but not editable:
    public DateOnly? DateOfBirth { get; set; }
    public bool HasProfilePicture { get; set; }
}

public class ErrorViewModel
{
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "Please try again in a moment.";
}

// ---------- Organiser ----------

public class OrganiserDashboardViewModel
{
    public List<OrganiserEventSummaryResponse> Events { get; set; } = new();
    public List<OrganiserEventSummaryResponse> Upcoming { get; set; } = new();
    public int TotalEnrolments { get; set; }
    public string? Error { get; set; }
}

public class OrganiserEventsViewModel
{
    public List<OrganiserEventSummaryResponse> Events { get; set; } = new();
    public string? Error { get; set; }
}

/// <summary>Used by both Create and Edit. The distance, fee and capacity only apply when creating (they make the first category).</summary>
public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), StringLength(100)]
    public string EventName { get; set; } = "";

    [Required(ErrorMessage = "Please choose the event type."), RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Please choose Run, Walk or Cycle.")]
    public string EventType { get; set; } = "Run";

    [Required(ErrorMessage = "Please choose the event date."), DataType(DataType.Date)]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(150)]
    public string Location { get; set; } = "";

    [StringLength(1000, ErrorMessage = "The description can be up to 1000 characters.")]
    public string? Description { get; set; }

    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    public decimal? DistanceKm { get; set; }

    [Range(1, 100000, ErrorMessage = "Places must be between 1 and 100000.")]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 999999.99, ErrorMessage = "The entry fee must be 0 or more.")]
    public decimal EntryFee { get; set; }

    public bool HasBanner { get; set; }
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Please enter a category name, e.g. 10km, 21km or Under 20."), StringLength(50)]
    public string CategoryName { get; set; } = "";

    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000, ErrorMessage = "Places must be between 1 and 100000.")]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 999999.99, ErrorMessage = "The entry fee must be 0 or more.")]
    public decimal EntryFee { get; set; }
}

public class CategoriesPageViewModel
{
    public EventDetailResponse Event { get; set; } = null!;
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EventEnrolmentsViewModel
{
    public EventDetailResponse Event { get; set; } = null!;
    public List<OrganiserEnrolmentResponse> Enrolments { get; set; } = new();
}

/// <summary>One row of the results-entry page.</summary>
public class ResultInputModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public bool HasResult { get; set; }
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
}
