using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public interface IRaceDayApi
{
    Task<ApiResponse<ApiAuthResponse>> LoginAsync(string email, string password);
    Task<ApiResponse<ApiProfile>> RegisterAsync(object request);

    Task<ApiResponse<ApiProfile>> GetProfileAsync();
    Task<ApiResponse<ApiProfile>> UpdateProfileAsync(object request);
    Task<ApiResponse<ApiProfile>> UploadProfilePictureAsync(IFormFile file);

    Task<ApiResponse<List<ApiEvent>>> GetEventsAsync(string? type, bool upcomingOnly, string? search);
    Task<ApiResponse<List<ApiEvent>>> GetMyEventsAsync();
    Task<ApiResponse<ApiEvent>> GetEventAsync(int id);
    Task<ApiResponse<ApiEvent>> CreateEventAsync(object request);
    Task<ApiResponse<ApiEvent>> UpdateEventAsync(int id, object request);
    Task<ApiResponse<object>> DeleteEventAsync(int id);
    Task<ApiResponse<ApiEvent>> UploadBannerAsync(int eventId, IFormFile file);

    Task<ApiResponse<List<ApiCategory>>> GetAllCategoriesAsync();
    Task<ApiResponse<List<ApiCategory>>> GetEventCategoriesAsync(int eventId);
    Task<ApiResponse<ApiCategory>> AddCategoryAsync(int eventId, string name, string? description);
    Task<ApiResponse<object>> RemoveCategoryAsync(int eventId, int categoryId);

    Task<ApiResponse<ApiEnrollment>> EnrolAsync(int eventId, int categoryId);
    Task<ApiResponse<List<ApiEnrollment>>> GetEventEnrollmentsAsync(int eventId);
    Task<ApiResponse<List<ApiEnrollment>>> GetMyEnrollmentsAsync();
    Task<ApiResponse<object>> WithdrawAsync(int enrollmentId);
    Task<ApiResponse<ApiEnrollment>> UpdateEnrollmentStatusAsync(int enrollmentId, string status);

    Task<ApiResponse<ApiRaceResult>> CreateResultAsync(int enrollmentId, TimeSpan finishTime, int position);
    Task<ApiResponse<ApiRaceResult>> UpdateResultAsync(int resultId, TimeSpan finishTime, int position);
    Task<ApiResponse<object>> DeleteResultAsync(int resultId);
    Task<ApiResponse<List<ApiRaceResult>>> GetEventResultsAsync(int eventId);
    Task<ApiResponse<List<ApiRaceResult>>> GetMyResultsAsync();
    Task<ApiResponse<ApiEvent>> SetPublicationAsync(int eventId, bool published);

    Task<(byte[] Bytes, string ContentType)?> GetImageAsync(string path);
}

public class RaceDayApi : IRaceDayApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string FriendlyFallback = "Something went wrong. Please try again.";
    private const string Unreachable = "RaceDay is temporarily unavailable. Please try again in a moment.";

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public RaceDayApi(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    public Task<ApiResponse<ApiAuthResponse>> LoginAsync(string email, string password) =>
        SendAsync<ApiAuthResponse>(HttpMethod.Post, "api/auth/login", Json(new { email, password }));

    public Task<ApiResponse<ApiProfile>> RegisterAsync(object request) =>
        SendAsync<ApiProfile>(HttpMethod.Post, "api/auth/register", Json(request));

    public Task<ApiResponse<ApiProfile>> GetProfileAsync() =>
        SendAsync<ApiProfile>(HttpMethod.Get, "api/users/me");

    public Task<ApiResponse<ApiProfile>> UpdateProfileAsync(object request) =>
        SendAsync<ApiProfile>(HttpMethod.Put, "api/users/me", Json(request));

    public Task<ApiResponse<ApiProfile>> UploadProfilePictureAsync(IFormFile file) =>
        SendAsync<ApiProfile>(HttpMethod.Post, "api/users/me/picture", FileForm(file));

    public Task<ApiResponse<List<ApiEvent>>> GetEventsAsync(string? type, bool upcomingOnly, string? search)
    {
        var query = new List<string> { $"upcomingOnly={(upcomingOnly ? "true" : "false")}" };
        if (!string.IsNullOrWhiteSpace(type))
        {
            query.Add($"type={Uri.EscapeDataString(type)}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search)}");
        }

        return SendAsync<List<ApiEvent>>(HttpMethod.Get, "api/events?" + string.Join("&", query));
    }

    public Task<ApiResponse<List<ApiEvent>>> GetMyEventsAsync() =>
        SendAsync<List<ApiEvent>>(HttpMethod.Get, "api/events/mine");

    public Task<ApiResponse<ApiEvent>> GetEventAsync(int id) =>
        SendAsync<ApiEvent>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResponse<ApiEvent>> CreateEventAsync(object request) =>
        SendAsync<ApiEvent>(HttpMethod.Post, "api/events", Json(request));

    public Task<ApiResponse<ApiEvent>> UpdateEventAsync(int id, object request) =>
        SendAsync<ApiEvent>(HttpMethod.Put, $"api/events/{id}", Json(request));

    public Task<ApiResponse<object>> DeleteEventAsync(int id) =>
        SendAsync<object>(HttpMethod.Delete, $"api/events/{id}");

    public Task<ApiResponse<ApiEvent>> UploadBannerAsync(int eventId, IFormFile file) =>
        SendAsync<ApiEvent>(HttpMethod.Post, $"api/events/{eventId}/banner", FileForm(file));

    public Task<ApiResponse<List<ApiCategory>>> GetAllCategoriesAsync() =>
        SendAsync<List<ApiCategory>>(HttpMethod.Get, "api/categories");

    public Task<ApiResponse<List<ApiCategory>>> GetEventCategoriesAsync(int eventId) =>
        SendAsync<List<ApiCategory>>(HttpMethod.Get, $"api/events/{eventId}/categories");

    public Task<ApiResponse<ApiCategory>> AddCategoryAsync(int eventId, string name, string? description) =>
        SendAsync<ApiCategory>(HttpMethod.Post, $"api/events/{eventId}/categories", Json(new { categoryName = name, description }));

    public Task<ApiResponse<object>> RemoveCategoryAsync(int eventId, int categoryId) =>
        SendAsync<object>(HttpMethod.Delete, $"api/events/{eventId}/categories/{categoryId}");

    public Task<ApiResponse<ApiEnrollment>> EnrolAsync(int eventId, int categoryId) =>
        SendAsync<ApiEnrollment>(HttpMethod.Post, $"api/events/{eventId}/enrollments", Json(new { categoryId }));

    public Task<ApiResponse<List<ApiEnrollment>>> GetEventEnrollmentsAsync(int eventId) =>
        SendAsync<List<ApiEnrollment>>(HttpMethod.Get, $"api/events/{eventId}/enrollments");

    public Task<ApiResponse<List<ApiEnrollment>>> GetMyEnrollmentsAsync() =>
        SendAsync<List<ApiEnrollment>>(HttpMethod.Get, "api/enrollments/mine");

    public Task<ApiResponse<object>> WithdrawAsync(int enrollmentId) =>
        SendAsync<object>(HttpMethod.Delete, $"api/enrollments/{enrollmentId}");

    public Task<ApiResponse<ApiEnrollment>> UpdateEnrollmentStatusAsync(int enrollmentId, string status) =>
        SendAsync<ApiEnrollment>(HttpMethod.Put, $"api/enrollments/{enrollmentId}/status", Json(new { status }));

    public Task<ApiResponse<ApiRaceResult>> CreateResultAsync(int enrollmentId, TimeSpan finishTime, int position) =>
        SendAsync<ApiRaceResult>(HttpMethod.Post, $"api/enrollments/{enrollmentId}/results", Json(new { finishTime = finishTime.ToString("c"), finishPosition = position }));

    public Task<ApiResponse<ApiRaceResult>> UpdateResultAsync(int resultId, TimeSpan finishTime, int position) =>
        SendAsync<ApiRaceResult>(HttpMethod.Put, $"api/results/{resultId}", Json(new { finishTime = finishTime.ToString("c"), finishPosition = position }));

    public Task<ApiResponse<object>> DeleteResultAsync(int resultId) =>
        SendAsync<object>(HttpMethod.Delete, $"api/results/{resultId}");

    public Task<ApiResponse<List<ApiRaceResult>>> GetEventResultsAsync(int eventId) =>
        SendAsync<List<ApiRaceResult>>(HttpMethod.Get, $"api/events/{eventId}/results");

    public Task<ApiResponse<List<ApiRaceResult>>> GetMyResultsAsync() =>
        SendAsync<List<ApiRaceResult>>(HttpMethod.Get, "api/results/mine");

    public Task<ApiResponse<ApiEvent>> SetPublicationAsync(int eventId, bool published) =>
        SendAsync<ApiEvent>(HttpMethod.Put, $"api/events/{eventId}/results/publication", Json(new { published }));

    public async Task<(byte[] Bytes, string ContentType)?> GetImageAsync(string path)
    {
        try
        {
            using var response = await _http.GetAsync("api/images/" + path);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return (bytes, contentType);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private static HttpContent Json(object body) => JsonContent.Create(body, body.GetType(), null, JsonOptions);

    private static HttpContent FileForm(IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream());
        stream.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(stream, "file", file.FileName);
        return form;
    }

    private string? CurrentToken() => _accessor.HttpContext?.User.FindFirst("api_token")?.Value;

    private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
    {
        var token = CurrentToken();

        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
                {
                    return new ApiResponse<T> { Success = true, StatusCode = (int)response.StatusCode };
                }

                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                return new ApiResponse<T> { Success = true, Data = data, StatusCode = (int)response.StatusCode };
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
            {
                throw new SessionExpiredException();
            }

            var body = await response.Content.ReadAsStringAsync();
            return new ApiResponse<T>
            {
                Success = false,
                StatusCode = (int)response.StatusCode,
                Error = FriendlyMessage((int)response.StatusCode, body)
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<T> { Success = false, StatusCode = 503, Error = Unreachable };
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse<T> { Success = false, StatusCode = 503, Error = Unreachable };
        }
        catch (JsonException)
        {
            return new ApiResponse<T> { Success = false, StatusCode = 502, Error = FriendlyFallback };
        }
    }

    private static string FriendlyMessage(int status, string body)
    {
        if (status >= 500)
        {
            return status == 503 ? Unreachable : FriendlyFallback;
        }

        if (status == 401)
        {
            return "Your email or password is incorrect.";
        }

        if (status == 403)
        {
            return "You do not have permission to do that.";
        }

        if (status == 404)
        {
            return "We could not find what you were looking for.";
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                var text = detail.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = new List<string>();
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var item in property.Value.EnumerateArray())
                    {
                        var message = item.GetString();
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            messages.Add(message);
                        }
                    }
                }

                if (messages.Count > 0)
                {
                    return "Please check your details and try again.";
                }
            }
        }
        catch (JsonException)
        {
        }

        return FriendlyFallback;
    }
}
