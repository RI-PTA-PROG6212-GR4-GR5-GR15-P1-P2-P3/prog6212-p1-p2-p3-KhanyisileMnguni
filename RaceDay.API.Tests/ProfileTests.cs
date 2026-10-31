using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;

namespace RaceDay.API.Tests;

public class ProfileTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Organiser_Profile_HasOrganisationName_AndNoDateOfBirth()
    {
        var organiser = await factory.OrganiserClient();

        var profile = await (await organiser.GetAsync("/api/profile")).Content.ReadFromJsonAsync<ProfileResponse>();

        Assert.Equal("Organiser", profile!.Role);
        Assert.Equal("Test Events", profile.OrganisationName);
        Assert.Null(profile.DateOfBirth);
    }

    [Fact]
    public async Task Participant_CanUpdateEmergencyContact()
    {
        var participant = await factory.ParticipantClient();
        var current = (await (await participant.GetAsync("/api/profile")).Content.ReadFromJsonAsync<ProfileResponse>())!;

        var res = await participant.PutAsJsonAsync("/api/profile", new
        {
            name = current.Name,
            email = current.Email,
            emergencyContact = "071 111 2222"
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("071 111 2222", (await res.Content.ReadFromJsonAsync<ProfileResponse>())!.EmergencyContact);

        // The change must really be saved, not just echoed back.
        var reloaded = await (await participant.GetAsync("/api/profile")).Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("071 111 2222", reloaded!.EmergencyContact);
    }

    [Fact]
    public async Task UpdateProfile_WithEmailOfAnotherUser_Returns409()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var organiserEmail = (await (await organiser.GetAsync("/api/profile")).Content.ReadFromJsonAsync<ProfileResponse>())!.Email;

        var res = await participant.PutAsJsonAsync("/api/profile", new { name = "Test Participant", email = organiserEmail });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_InvalidEmail_Returns400()
    {
        var participant = await factory.ParticipantClient();

        var res = await participant.PutAsJsonAsync("/api/profile", new { name = "Someone", email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().PutAsJsonAsync("/api/profile", new { name = "X", email = "x@test.co.za" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Register_SameEmailInDifferentCase_Returns409()
    {
        var client = factory.CreateClient();
        var email = Api.UniqueEmail();
        var first = await client.PostAsJsonAsync("/api/auth/register",
            new { name = "A", email, password = Api.Password, role = "Organiser" });
        var second = await client.PostAsJsonAsync("/api/auth/register",
            new { name = "B", email = email.ToUpperInvariant(), password = Api.Password, role = "Organiser" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_EmailIsNotCaseSensitive()
    {
        var client = factory.CreateClient();
        var email = Api.UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { name = "Case", email, password = Api.Password, role = "Organiser" });

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { email = email.ToUpperInvariant(), password = Api.Password });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Login_ResponseNeverContainsThePassword()
    {
        var client = factory.CreateClient();
        var email = Api.UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { name = "Safe", email, password = Api.Password, role = "Organiser" });

        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Api.Password });
        var text = await res.Content.ReadAsStringAsync();

        Assert.DoesNotContain(Api.Password, text);
    }
}

