using System.Net;
using System.Net.Http.Json;

namespace RaceDay.API.Tests;

public class LogoutTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Organiser_CanLogout_Returns204()
    {
        var organiser = await factory.OrganiserClient();
        Assert.Equal(HttpStatusCode.NoContent, (await organiser.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task Participant_CanLogout_Returns204()
    {
        var participant = await factory.ParticipantClient();
        Assert.Equal(HttpStatusCode.NoContent, (await participant.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task Token_StopsWorking_AfterLogout()
    {
        var organiser = await factory.OrganiserClient();
        Assert.Equal(HttpStatusCode.OK, (await organiser.GetAsync("/api/profile")).StatusCode);

        await organiser.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await organiser.GetAsync("/api/profile")).StatusCode);
    }

    [Fact]
    public async Task Logout_Twice_SecondCallIsRejected()
    {
        var participant = await factory.ParticipantClient();

        Assert.Equal(HttpStatusCode.NoContent, (await participant.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await participant.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutLoggingIn_Returns401()
    {
        var res = await factory.CreateClient().PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Logout_OnlyEndsThatOneToken_AnotherLoginOfTheSameUserKeepsWorking()
    {
        var email = Api.UniqueEmail();
        var phone = factory.CreateClient();
        await phone.PostAsJsonAsync("/api/auth/register", new { name = "Two Logins", email, password = Api.Password, role = "Organiser" });
        var laptop = factory.CreateClient();
        await Api.SignIn(phone, email);
        await Api.SignIn(laptop, email);

        await phone.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await phone.GetAsync("/api/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await laptop.GetAsync("/api/profile")).StatusCode);
    }

    [Fact]
    public async Task LoggingInAgain_AfterLogout_GivesAWorkingToken()
    {
        var email = Api.UniqueEmail();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new { name = "Back Again", email, password = Api.Password, role = "Organiser" });
        await Api.SignIn(client, email);
        await client.PostAsync("/api/auth/logout", null);

        await Api.SignIn(client, email);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/profile")).StatusCode);
    }
}
