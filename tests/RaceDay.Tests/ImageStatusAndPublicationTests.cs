using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class ImageStatusAndPublicationTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ImageStatusAndPublicationTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent ImageForm(string contentType = "image/png", string fileName = "image.png", int size = 16)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Enumerable.Range(0, size).Select(i => (byte)i).ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Organiser_CanUploadBanner_AndItCanBeDownloadedThroughTheApi()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        using var client = _factory.CreateClientFor(organiser.Token);

        var upload = await client.PostAsync($"/api/events/{eventId}/banner", ImageForm());

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var json = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var path = json.GetProperty("bannerImage").GetString();
        Assert.StartsWith("banners/", path);

        using var anonymous = _factory.CreateClient();
        var download = await anonymous.GetAsync($"/api/images/{path}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UploadBanner_WithWrongFileType_Returns400()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsync($"/api/events/{eventId}/banner", ImageForm("application/pdf", "doc.pdf"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithoutFile_Returns400()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsync($"/api/events/{eventId}/banner", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUploadBanner_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        using var client = _factory.CreateClientFor(participant.Token);

        var response = await client.PostAsync($"/api/events/{eventId}/banner", ImageForm());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrganiserWhoDoesNotOwnTheEvent_CannotUploadBanner_Returns403()
    {
        var owner = await _factory.RegisterAndLoginAsync("Organiser");
        var other = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(owner.Token);
        using var client = _factory.CreateClientFor(other.Token);

        var response = await client.PostAsync($"/api/events/{eventId}/banner", ImageForm());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanUploadProfilePicture()
    {
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        using var client = _factory.CreateClientFor(participant.Token);

        var response = await client.PostAsync("/api/users/me/picture", ImageForm("image/jpeg", "me.jpg"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("profiles/", json.GetProperty("profilePicture").GetString());
    }

    [Fact]
    public async Task Organiser_CannotUploadProfilePicture_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsync("/api/users/me/picture", ImageForm());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetImage_UnknownPathOrOutsideAllowedFolders_Returns404()
    {
        using var client = _factory.CreateClient();

        var missing = await client.GetAsync("/api/images/banners/does-not-exist.png");
        var outside = await client.GetAsync("/api/images/secrets/file.png");

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, outside.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanConfirmAndCancelEnrollment_AndStatusIsVisibleToParticipant()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var organiserClient = _factory.CreateClientFor(organiser.Token);

        var confirm = await organiserClient.PutAsJsonAsync($"/api/enrollments/{enrollmentId}/status", new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        using var participantClient = _factory.CreateClientFor(participant.Token);
        var mine = await participantClient.GetAsync("/api/enrollments/mine");
        var list = await mine.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(list.EnumerateArray(), e => e.GetProperty("status").GetString() == "Confirmed");

        var cancel = await organiserClient.PutAsJsonAsync($"/api/enrollments/{enrollmentId}/status", new { status = "Cancelled" });
        var cancelled = await cancel.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_Returns400()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PutAsJsonAsync($"/api/enrollments/{enrollmentId}/status", new { status = "Maybe" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotChangeEnrollmentStatus_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(participant.Token);

        var response = await client.PutAsJsonAsync($"/api/enrollments/{enrollmentId}/status", new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Results_AreHiddenFromParticipant_UntilPublished()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var organiserClient = _factory.CreateClientFor(organiser.Token);
        await organiserClient.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", new { finishTime = "00:40:00", finishPosition = 1 });
        using var participantClient = _factory.CreateClientFor(participant.Token);

        var before = await (await participantClient.GetAsync("/api/results/mine")).Content.ReadFromJsonAsync<JsonElement>();
        await _factory.PublishResultsAsync(organiser.Token, eventId);
        var after = await (await participantClient.GetAsync("/api/results/mine")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, before.GetArrayLength());
        Assert.Equal(1, after.GetArrayLength());
    }

    [Fact]
    public async Task PublishedResults_IncludeTotalFinishers()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var runnerOne = await _factory.RegisterAndLoginAsync("Participant");
        var runnerTwo = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrolOne = await _factory.EnrolAsync(runnerOne.Token, eventId, categoryId);
        var enrolTwo = await _factory.EnrolAsync(runnerTwo.Token, eventId, categoryId);
        using var organiserClient = _factory.CreateClientFor(organiser.Token);
        await organiserClient.PostAsJsonAsync($"/api/enrollments/{enrolOne}/results", new { finishTime = "00:40:00", finishPosition = 1 });
        await organiserClient.PostAsJsonAsync($"/api/enrollments/{enrolTwo}/results", new { finishTime = "00:45:00", finishPosition = 2 });
        await _factory.PublishResultsAsync(organiser.Token, eventId);
        using var client = _factory.CreateClientFor(runnerTwo.Token);

        var results = await (await client.GetAsync("/api/results/mine")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(2, results[0].GetProperty("finishPosition").GetInt32());
        Assert.Equal(2, results[0].GetProperty("totalFinishers").GetInt32());
    }

    [Fact]
    public async Task Participant_CannotPublishResults_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        using var client = _factory.CreateClientFor(participant.Token);

        var response = await client.PutAsJsonAsync($"/api/events/{eventId}/results/publication", new { published = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrganiserWhoDoesNotOwnTheEvent_CannotPublishResults_Returns403()
    {
        var owner = await _factory.RegisterAndLoginAsync("Organiser");
        var other = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(owner.Token);
        using var client = _factory.CreateClientFor(other.Token);

        var response = await client.PutAsJsonAsync($"/api/events/{eventId}/results/publication", new { published = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
