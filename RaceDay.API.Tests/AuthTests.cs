using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Register_Organiser_Returns201WithRole()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Sarah",
            email = Api.UniqueEmail(),
            password = Api.Password,
            role = "Organiser"
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("Organiser", body!.Role);
        Assert.True(body.UserId > 0);
    }

    [Fact]
    public async Task Register_Participant_Returns201()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Thandi",
            email = Api.UniqueEmail(),
            password = Api.Password,
            role = "Participant",
            dateOfBirth = "1995-05-24"
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = factory.CreateClient();
        var body = new { name = "A", email = Api.UniqueEmail(), password = Api.Password, role = "Organiser" };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", body)).StatusCode);
    }

    [Fact]
    public async Task Register_ParticipantWithoutDateOfBirth_Returns400()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            name = "No DOB",
            email = Api.UniqueEmail(),
            password = Api.Password,
            role = "Participant"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            name = "Hacker",
            email = Api.UniqueEmail(),
            password = Api.Password,
            role = "Admin"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            name = "Weak",
            email = Api.UniqueEmail(),
            password = "abc",
            role = "Organiser"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200AndRole()
    {
        var client = factory.CreateClient();
        var email = Api.UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new { name = "L", email, password = Api.Password, role = "Organiser" });

        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Api.Password });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("Organiser", body!.Role);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.True(body.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = factory.CreateClient();
        var email = Api.UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new { name = "L", email, password = Api.Password, role = "Organiser" });

        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@nowhere.co.za", password = Api.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Profile_WhenNotLoggedIn_Returns401()
    {
        var res = await factory.CreateClient().GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Profile_WhenLoggedIn_ReturnsOwnProfile()
    {
        var client = await factory.ParticipantClient();
        var res = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var profile = await res.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("Participant", profile!.Role);
        Assert.Equal("082 555 0102", profile.EmergencyContact);
    }

    [Fact]
    public async Task Profile_Update_ChangesName()
    {
        var client = await factory.OrganiserClient();
        var current = (await (await client.GetAsync("/api/profile")).Content.ReadFromJsonAsync<ProfileResponse>())!;

        var res = await client.PutAsJsonAsync("/api/profile", new
        {
            name = "Renamed Organiser",
            email = current.Email,
            organisationName = "New Org"
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Renamed Organiser", (await res.Content.ReadFromJsonAsync<ProfileResponse>())!.Name);
    }

    [Fact]
    public async Task Login_Token_GrantsAccessToProtectedEndpoint()
    {
        var client = factory.CreateClient();
        var email = Api.UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new { name = "T", email, password = Api.Password, role = "Organiser" });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);

        await Api.SignIn(client, email);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/profile")).StatusCode);
    }

    [Fact]
    public async Task Profile_WithInvalidToken_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not.a.real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
    }

    [Fact]
    public async Task Profile_WithTamperedToken_Returns401()
    {
        var client = await factory.ParticipantClient();
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        // Flip the last character of the signature so it no longer matches.
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tampered);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
    }

    [Fact]
    public async Task Token_RoleCannotBeSwappedByTheClient_ParticipantStaysForbiddenFromOrganiserEndpoints()
    {
        var participant = await factory.ParticipantClient();
        var res = await participant.PostAsJsonAsync("/api/events", Api.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
