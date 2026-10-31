using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Services;

namespace RaceDay.API.Tests;

/// <summary>Event banners and profile pictures. Azure is replaced by an in-memory fake.</summary>
public class ImageUploadTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // ---------- Event banner ----------

    [Fact]
    public async Task Owner_CanUploadBanner_AndTheEventNowHasOne()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);

        var res = await Api.UploadImage(organiser, $"/api/events/{eventId}/banner", Api.PngBytes());

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<ImageUploadResponse>();
        Assert.True(body!.HasImage);
        Assert.Equal($"/api/events/{eventId}/banner", body.Url);

        var detail = await factory.CreateClient().GetFromJsonAsync<EventDetailResponse>($"/api/events/{eventId}");
        Assert.True(detail!.HasBanner);
    }

    [Fact]
    public async Task NewEvent_HasNoBannerYet()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);

        var detail = await factory.CreateClient().GetFromJsonAsync<EventDetailResponse>($"/api/events/{eventId}");

        Assert.False(detail!.HasBanner);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().GetAsync($"/api/events/{eventId}/banner")).StatusCode);
    }

    [Fact]
    public async Task Banner_CanBeViewedByAnyone_AndIsTheSameImage()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);
        var png = Api.PngBytes(200);
        await Api.UploadImage(organiser, $"/api/events/{eventId}/banner", png);

        var res = await factory.CreateClient().GetAsync($"/api/events/{eventId}/banner");   // not logged in

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("image/png", res.Content.Headers.ContentType!.MediaType);
        Assert.Equal(png, await res.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task UploadingASecondBanner_ReplacesTheFirst_AndRemovesTheOldFile()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);
        var before = factory.Blobs.CountIn(BlobContainers.EventBanners);

        await Api.UploadImage(organiser, $"/api/events/{eventId}/banner", Api.PngBytes(10));
        await Api.UploadImage(organiser, $"/api/events/{eventId}/banner", Api.PngBytes(20));

        Assert.Equal(before + 1, factory.Blobs.CountIn(BlobContainers.EventBanners));
    }

    [Fact]
    public async Task DeletingTheEvent_AlsoDeletesItsBannerFile()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);
        var before = factory.Blobs.CountIn(BlobContainers.EventBanners);
        await Api.UploadImage(organiser, $"/api/events/{eventId}/banner", Api.PngBytes());
        Assert.Equal(before + 1, factory.Blobs.CountIn(BlobContainers.EventBanners));

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/events/{eventId}")).StatusCode);

        Assert.Equal(before, factory.Blobs.CountIn(BlobContainers.EventBanners));
    }

    [Fact]
    public async Task OtherOrganiser_CannotUploadBanner_Returns403()
    {
        var owner = await factory.OrganiserClient();
        var other = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(owner);

        var res = await Api.UploadImage(other, $"/api/events/{eventId}/banner", Api.PngBytes());

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUploadBanner_Returns403()
    {
        var organiser = await factory.OrganiserClient();
        var participant = await factory.ParticipantClient();
        var eventId = await Api.CreateEvent(organiser);

        Assert.Equal(HttpStatusCode.Forbidden, (await Api.UploadImage(participant, $"/api/events/{eventId}/banner", Api.PngBytes())).StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithoutLogin_Returns401()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);

        var res = await Api.UploadImage(factory.CreateClient(), $"/api/events/{eventId}/banner", Api.PngBytes());

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ForMissingEvent_Returns404()
    {
        var organiser = await factory.OrganiserClient();
        Assert.Equal(HttpStatusCode.NotFound, (await Api.UploadImage(organiser, "/api/events/999999/banner", Api.PngBytes())).StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithATextFile_Returns400()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);

        var res = await Api.UploadImage(organiser, $"/api/events/{eventId}/banner",
            System.Text.Encoding.UTF8.GetBytes("just some text"), "notes.txt", "text/plain");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ThatIsNamedPngButIsNotAnImage_Returns400()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);

        var res = await Api.UploadImage(organiser, $"/api/events/{eventId}/banner",
            System.Text.Encoding.UTF8.GetBytes("this is not really a picture"), "fake.png", "image/png");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_OverFiveMegabytes_Returns400()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);
        var tooBig = Api.PngBytes((int)ImageRules.MaxBytes);   // PNG header + 5 MB

        var res = await Api.UploadImage(organiser, $"/api/events/{eventId}/banner", tooBig);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task RejectedUpload_LeavesNothingInStorage()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);
        var before = factory.Blobs.CountIn(BlobContainers.EventBanners);

        await Api.UploadImage(organiser, $"/api/events/{eventId}/banner",
            System.Text.Encoding.UTF8.GetBytes("nope"), "notes.txt", "text/plain");

        Assert.Equal(before, factory.Blobs.CountIn(BlobContainers.EventBanners));
    }

    [Fact]
    public async Task TheBannerUrl_IsSavedInTheDatabase()
    {
        var organiser = await factory.OrganiserClient();
        var eventId = await Api.CreateEvent(organiser);

        await Api.UploadImage(organiser, $"/api/events/{eventId}/banner", Api.PngBytes());

        using var scope = factory.Services.CreateScope();
        var saved = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Events.FindAsync(eventId);
        Assert.StartsWith("https://", saved!.BannerUrl);
        Assert.Contains(BlobContainers.EventBanners, saved.BannerUrl);
        Assert.EndsWith(".png", saved.BannerUrl);
    }

    // ---------- Participant profile picture ----------

    [Fact]
    public async Task Participant_CanUploadProfilePicture_AndProfileShowsIt()
    {
        var participant = await factory.ParticipantClient();
        var profileBefore = await participant.GetFromJsonAsync<ProfileResponse>("/api/profile");
        Assert.False(profileBefore!.HasProfilePicture);

        var res = await Api.UploadImage(participant, "/api/profile/picture", Api.PngBytes());

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var profileAfter = await participant.GetFromJsonAsync<ProfileResponse>("/api/profile");
        Assert.True(profileAfter!.HasProfilePicture);
    }

    [Fact]
    public async Task TheProfilePictureUrl_IsSavedInTheDatabase()
    {
        var participant = await factory.ParticipantClient();
        var me = await participant.GetFromJsonAsync<ProfileResponse>("/api/profile");

        await Api.UploadImage(participant, "/api/profile/picture", Api.PngBytes());

        using var scope = factory.Services.CreateScope();
        var saved = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Participants.FindAsync(me!.UserId);
        Assert.StartsWith("https://", saved!.ProfilePictureUrl);
        Assert.Contains(BlobContainers.ProfilePictures, saved.ProfilePictureUrl);
    }

    [Fact]
    public async Task Participant_CanDownloadTheirOwnPicture()
    {
        var participant = await factory.ParticipantClient();
        var png = Api.PngBytes(120);
        await Api.UploadImage(participant, "/api/profile/picture", png);

        var res = await participant.GetAsync("/api/profile/picture");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(png, await res.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task ProfilePicture_BeforeUploading_Returns404()
    {
        var participant = await factory.ParticipantClient();
        Assert.Equal(HttpStatusCode.NotFound, (await participant.GetAsync("/api/profile/picture")).StatusCode);
    }

    [Fact]
    public async Task ReplacingTheProfilePicture_RemovesTheOldFile()
    {
        var participant = await factory.ParticipantClient();
        var before = factory.Blobs.CountIn(BlobContainers.ProfilePictures);

        await Api.UploadImage(participant, "/api/profile/picture", Api.PngBytes(10));
        await Api.UploadImage(participant, "/api/profile/picture", Api.PngBytes(30));

        Assert.Equal(before + 1, factory.Blobs.CountIn(BlobContainers.ProfilePictures));
    }

    [Fact]
    public async Task ProfilePicture_WithANonImage_Returns400()
    {
        var participant = await factory.ParticipantClient();
        var res = await Api.UploadImage(participant, "/api/profile/picture",
            System.Text.Encoding.UTF8.GetBytes("hello"), "cv.pdf", "application/pdf");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUseTheParticipantProfilePictureEndpoints_Returns403()
    {
        var organiser = await factory.OrganiserClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await Api.UploadImage(organiser, "/api/profile/picture", Api.PngBytes())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/profile/picture")).StatusCode);
    }

    [Fact]
    public async Task ProfilePicture_WithoutLogin_Returns401()
    {
        var res = await Api.UploadImage(factory.CreateClient(), "/api/profile/picture", Api.PngBytes());
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task OneParticipantsPicture_IsNotVisibleToAnotherParticipant()
    {
        var first = await factory.ParticipantClient();
        var second = await factory.ParticipantClient();
        await Api.UploadImage(first, "/api/profile/picture", Api.PngBytes());

        // The second participant only ever sees their own picture, and they have none.
        Assert.Equal(HttpStatusCode.NotFound, (await second.GetAsync("/api/profile/picture")).StatusCode);
    }
}
