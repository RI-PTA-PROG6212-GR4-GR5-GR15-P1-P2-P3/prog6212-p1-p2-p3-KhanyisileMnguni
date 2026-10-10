public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? OrganisationName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? EmergencyContact { get; set; }
}