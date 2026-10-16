using RaceDay.API.Models;

namespace RaceDay.API.Dtos;

/// <summary>Reusable projections so controllers return DTOs, never entities (and never password hashes).</summary>
public static class Mapping
{
    public static IQueryable<EventResponse> ToResponse(this IQueryable<Event> q) =>
        q.Select(e => new EventResponse(e.EventID, e.EventName, e.EventType, e.EventDate, e.Location,
            e.Description, e.Organiser.Name, e.Organiser.OrganisationName));

    public static IQueryable<CategoryResponse> ToResponse(this IQueryable<Category> q) =>
        q.Select(c => new CategoryResponse(c.CategoryID, c.EventID, c.CategoryName, c.DistanceKM,
            c.MaxParticipants, c.EntryFee, c.Enrolments.Count()));

    public static IQueryable<EnrolmentResponse> ToResponse(this IQueryable<Enrolment> q) =>
        q.Select(n => new EnrolmentResponse(n.EnrolmentID, n.EnrolmentDate, n.Status,
            n.CategoryID, n.Category.CategoryName, n.Category.DistanceKM, n.Category.EntryFee,
            n.Category.EventID, n.Category.Event.EventName, n.Category.Event.EventDate, n.Category.Event.Location));

    public static ResultResponse ToResponse(this Result r) =>
        new(r.ResultID, r.EnrolmentID, r.FinishTime, r.Position, r.Status);

    /// <summary>"run" / "RUN" -> "Run"; keeps stored values consistent.</summary>
    public static string Normalise(string value)
    {
        value = value.Trim();
        return value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }

    /// <summary>"dnf" -> "DNF"; everything else is title-cased.</summary>
    public static string NormaliseResultStatus(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "dnf" => "DNF",
            "dns" => "DNS",
            var other => Normalise(other)
        };
}
