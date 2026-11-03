using System.Globalization;

namespace RaceDay.MVC.Infrastructure;

/// <summary>Small formatting helpers so every page shows statuses, times and prices the same way.</summary>
public static class UiHelpers
{
    /// <summary>CSS class for a coloured status badge (amber = pending, green = confirmed/completed, red = problem).</summary>
    public static string StatusClass(string? status) => (status ?? "").Trim().ToLowerInvariant() switch
    {
        "registered" or "pending" => "status-pending",
        "confirmed" or "completed" or "finished" => "status-confirmed",
        "dnf" or "dns" or "disqualified" or "cancelled" => "status-problem",
        _ => "status-neutral"
    };

    public static string StatusLabel(string? status) => (status ?? "").Trim().ToLowerInvariant() switch
    {
        "dnf" => "Did not finish",
        "dns" => "Did not start",
        "" => "Unknown",
        _ => status!.Trim()
    };

    public static string TypeIcon(string? eventType) => (eventType ?? "").ToLowerInvariant() switch
    {
        "walk" => "bi-person-walking",
        "cycle" => "bi-bicycle",
        _ => "bi-lightning-charge-fill"
    };

    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13) return $"{number}th";
        return (number % 10) switch { 1 => $"{number}st", 2 => $"{number}nd", 3 => $"{number}rd", _ => $"{number}th" };
    }

    public static string FormatTime(TimeSpan? time) =>
        time is null ? "-" : time.Value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

    public static string Money(decimal amount) =>
        amount == 0 ? "Free" : "R" + amount.ToString("N2", CultureInfo.InvariantCulture);

    public static string Date(DateOnly date) => date.ToString("ddd, d MMM yyyy", CultureInfo.InvariantCulture);
}
