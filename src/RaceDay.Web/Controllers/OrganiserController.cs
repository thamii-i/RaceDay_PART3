using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = "Organiser")]
public class OrganiserController : Controller
{
    private readonly IRaceDayApi _api;

    public OrganiserController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var response = await _api.GetMyEventsAsync();
        if (!response.Success)
        {
            TempData["Error"] = response.Error;
        }

        var events = response.Data ?? new List<ApiEvent>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var model = new DashboardViewModel
        {
            Events = events,
            Upcoming = events.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).ToList(),
            TotalEnrollments = events.Sum(e => e.EnrollmentCount)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Events()
    {
        var response = await _api.GetMyEventsAsync();
        if (!response.Success)
        {
            TempData["Error"] = response.Error;
        }

        return View(response.Data ?? new List<ApiEvent>());
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        return View("EventForm", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30) });
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        var response = await _api.CreateEventAsync(ToRequest(model));
        if (!response.Success || response.Data is null)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "We could not create the event.");
            return View("EventForm", model);
        }

        if (model.BannerFile is { Length: > 0 })
        {
            var upload = await _api.UploadBannerAsync(response.Data.EventId, model.BannerFile);
            if (!upload.Success)
            {
                TempData["Warning"] = $"The event was created, but the banner could not be uploaded: {upload.Error}";
                return RedirectToAction(nameof(EditEvent), new { id = response.Data.EventId });
            }
        }

        TempData["Success"] = "Event created. Now add categories so participants can enrol.";
        return RedirectToAction(nameof(Categories), new { eventId = response.Data.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var response = await _api.GetEventAsync(id);
        if (!response.Success || response.Data is null)
        {
            TempData["Error"] = response.Error ?? "We could not load that event.";
            return RedirectToAction(nameof(Events));
        }

        var ev = response.Data;
        return View("EventForm", new EventFormViewModel
        {
            EventId = ev.EventId,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate.ToDateTime(TimeOnly.MinValue),
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            ExistingBanner = ev.BannerImage
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.EventId = id;

        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        var response = await _api.UpdateEventAsync(id, ToRequest(model));
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "We could not save the event.");
            return View("EventForm", model);
        }

        if (model.BannerFile is { Length: > 0 })
        {
            var upload = await _api.UploadBannerAsync(id, model.BannerFile);
            if (!upload.Success)
            {
                TempData["Warning"] = $"The event was saved, but the banner could not be uploaded: {upload.Error}";
                return RedirectToAction(nameof(EditEvent), new { id });
            }
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var response = await _api.DeleteEventAsync(id);
        if (response.Success)
        {
            TempData["Success"] = "Event deleted.";
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data is null)
        {
            TempData["Error"] = ev.Error ?? "We could not load that event.";
            return RedirectToAction(nameof(Events));
        }

        var assigned = await _api.GetEventCategoriesAsync(eventId);
        var all = await _api.GetAllCategoriesAsync();
        var assignedList = assigned.Data ?? new List<ApiCategory>();

        return View(new CategoriesPageViewModel
        {
            Event = ev.Data,
            Categories = assignedList,
            AllCategories = (all.Data ?? new List<ApiCategory>()).Where(c => assignedList.All(a => a.CategoryId != c.CategoryId)).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int eventId, string? categoryName, string? description)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            TempData["Error"] = "Please enter or choose a category name.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        if (categoryName.Trim().Length > 50)
        {
            TempData["Error"] = "Category names can be at most 50 characters long.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        var response = await _api.AddCategoryAsync(eventId, categoryName.Trim(), string.IsNullOrWhiteSpace(description) ? null : description.Trim());
        if (response.Success)
        {
            TempData["Success"] = $"Category '{response.Data?.CategoryName}' added to the event.";
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCategory(int eventId, int categoryId)
    {
        var response = await _api.RemoveCategoryAsync(eventId, categoryId);
        if (response.Success)
        {
            TempData["Success"] = "Category removed from the event.";
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Enrollments(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data is null)
        {
            TempData["Error"] = ev.Error ?? "We could not load that event.";
            return RedirectToAction(nameof(Events));
        }

        var enrollments = await _api.GetEventEnrollmentsAsync(eventId);
        if (!enrollments.Success)
        {
            TempData["Error"] = enrollments.Error;
        }

        return View(new EnrollmentsPageViewModel
        {
            Event = ev.Data,
            Enrollments = enrollments.Data ?? new List<ApiEnrollment>()
        });
    }

    [HttpPost]
    public async Task<IActionResult> SetEnrollmentStatus(int eventId, int enrollmentId, string status)
    {
        var response = await _api.UpdateEnrollmentStatusAsync(enrollmentId, status);
        if (response.Success)
        {
            TempData["Success"] = $"Enrollment marked as {status}.";
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return RedirectToAction(nameof(Enrollments), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data is null)
        {
            TempData["Error"] = ev.Error ?? "We could not load that event.";
            return RedirectToAction(nameof(Events));
        }

        var enrollments = await _api.GetEventEnrollmentsAsync(eventId);
        var results = await _api.GetEventResultsAsync(eventId);
        if (!enrollments.Success)
        {
            TempData["Error"] = enrollments.Error;
        }

        var resultList = results.Data ?? new List<ApiRaceResult>();
        var rows = (enrollments.Data ?? new List<ApiEnrollment>())
            .Where(e => e.Status != "Cancelled")
            .Select(e => new ResultRow { Enrollment = e, Result = resultList.FirstOrDefault(r => r.EnrollmentId == e.EnrollmentId) })
            .OrderBy(r => r.Result is null ? int.MaxValue : r.Result.FinishPosition)
            .ThenBy(r => r.Enrollment.BibNumber)
            .ToList();

        return View(new ResultsPageViewModel
        {
            Event = ev.Data,
            Rows = rows,
            IsPast = ev.Data.EventDate < DateOnly.FromDateTime(DateTime.Today)
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(int eventId, int enrollmentId, int? resultId, string? finishTime, int? finishPosition)
    {
        if (!TimeSpan.TryParse(finishTime, out var time) || time <= TimeSpan.Zero)
        {
            TempData["Error"] = "Please enter a finish time in the format hh:mm:ss, for example 00:48:12.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        if (finishPosition is null or < 1)
        {
            TempData["Error"] = "Please enter a finishing position of 1 or higher.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var response = resultId.HasValue
            ? await _api.UpdateResultAsync(resultId.Value, time, finishPosition.Value)
            : await _api.CreateResultAsync(enrollmentId, time, finishPosition.Value);

        if (response.Success)
        {
            TempData["Success"] = "Result saved.";
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return RedirectToAction(nameof(Results), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteResult(int eventId, int resultId)
    {
        var response = await _api.DeleteResultAsync(resultId);
        if (response.Success)
        {
            TempData["Success"] = "Result removed.";
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return RedirectToAction(nameof(Results), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Publication(int eventId, bool publish)
    {
        var response = await _api.SetPublicationAsync(eventId, publish);
        if (response.Success)
        {
            TempData["Success"] = publish
                ? "Results published. Participants can now see their results."
                : "Results hidden from participants.";
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return RedirectToAction(nameof(Results), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var response = await _api.GetProfileAsync();
        if (!response.Success || response.Data is null)
        {
            TempData["Error"] = response.Error ?? "We could not load your profile.";
            return RedirectToAction(nameof(Dashboard));
        }

        var profile = response.Data;
        return View(new OrganiserProfileViewModel
        {
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Email = profile.Email,
            Phone = profile.Phone,
            OrganisationName = profile.OrganisationName ?? string.Empty
        });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(OrganiserProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _api.UpdateProfileAsync(new
        {
            firstName = model.FirstName.Trim(),
            lastName = model.LastName.Trim(),
            phone = model.Phone.Trim(),
            organisationName = model.OrganisationName.Trim()
        });

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "We could not save your changes.");
            return View(model);
        }

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    private static object ToRequest(EventFormViewModel model) => new
    {
        eventName = model.EventName.Trim(),
        description = model.Description.Trim(),
        eventDate = model.EventDate!.Value.ToString("yyyy-MM-dd"),
        location = model.Location.Trim(),
        distanceKm = model.DistanceKm!.Value,
        eventType = model.EventType
    };
}
