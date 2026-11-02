namespace RaceDay.MVC.Models.Api;

// These records describe the JSON the Part 2 API sends back. They contain no logic.

public record LoginResponse(int UserId, string Role, string Name, string Token, DateTime ExpiresAtUtc);

public record AuthResponse(int UserId, string Role, string Name);

public record ProfileResponse(int UserId, string Role, string Name, string Email,
    string? OrganisationName, DateOnly? DateOfBirth, string? EmergencyContact, bool HasProfilePicture);

public record ImageUploadResponse(bool HasImage, string Url);

public record EventResponse(int EventId, string EventName, string EventType, DateOnly EventDate,
    string Location, string? Description, string OrganiserName, string? OrganisationName, bool HasBanner);

public record CategoryResponse(int CategoryId, int EventId, string CategoryName, decimal DistanceKm,
    int MaxParticipants, decimal EntryFee, int EnrolledCount);

public record EventDetailResponse(int EventId, string EventName, string EventType, DateOnly EventDate,
    string Location, string? Description, string OrganiserName, string? OrganisationName,
    IReadOnlyList<CategoryResponse> Categories, bool HasBanner);

public record EnrolmentResponse(int EnrolmentId, DateOnly EnrolmentDate, string Status,
    int CategoryId, string CategoryName, decimal DistanceKm, decimal EntryFee,
    int EventId, string EventName, DateOnly EventDate, string Location);

public record HistoryItemResponse(int ResultId, int EnrolmentId, int EventId, string EventName,
    DateOnly EventDate, string Location, string CategoryName, decimal DistanceKm,
    TimeSpan? FinishTime, int? Position, string Status, int CategoryId, int TotalFinishers);

// ---- Organiser ----

public record OrganiserEventSummaryResponse(int EventId, string EventName, string EventType, DateOnly EventDate,
    string Location, int CategoryCount, int EnrolmentCount, bool HasBanner);

public record OrganiserEnrolmentResponse(int EnrolmentId, int ParticipantId, string ParticipantName,
    string ParticipantEmail, string? EmergencyContact, int CategoryId, string CategoryName,
    DateOnly EnrolmentDate, string Status, bool HasResult, TimeSpan? FinishTime, int? Position, string? ResultStatus);

public record ResultResponse(int ResultId, int EnrolmentId, TimeSpan? FinishTime, int? Position, string Status);

public record CreatedEventResponse(int EventId);

public record CreatedCategoryResponse(int CategoryId);
