using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = "Participant")]
public class ParticipantController : Controller
{
    private readonly IRaceDayApi _api;

    public ParticipantController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> EventDetails(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (!ev.Success || ev.Data is null)
        {
            TempData["Error"] = ev.Error ?? "We could not load that event.";
            return RedirectToAction("Index", "Home");
        }

        var categories = await _api.GetEventCategoriesAsync(id);
        var mine = await _api.GetMyEnrollmentsAsync();

        var model = new EventDetailsViewModel
        {
            Event = ev.Data,
            Categories = categories.Data ?? new List<ApiCategory>(),
            MyEnrollment = mine.Data?.FirstOrDefault(e => e.EventId == id),
            IsPast = ev.Data.EventDate < DateOnly.FromDateTime(DateTime.Today)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            TempData["Error"] = "Please choose a category before enrolling.";
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        var response = await _api.EnrolAsync(eventId, categoryId);
        if (response.Success)
        {
            TempData["Success"] = $"You are enrolled! Your bib number is {response.Data?.BibNumber}. The organiser will confirm your entry.";
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return RedirectToAction(nameof(EventDetails), new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> MyEnrollments()
    {
        var response = await _api.GetMyEnrollmentsAsync();
        if (!response.Success)
        {
            TempData["Error"] = response.Error;
        }

        return View(response.Data ?? new List<ApiEnrollment>());
    }

    [HttpPost]
    public async Task<IActionResult> Withdraw(int id)
    {
        var response = await _api.WithdrawAsync(id);
        if (response.Success)
        {
            TempData["Success"] = "You have been withdrawn from the event.";
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return RedirectToAction(nameof(MyEnrollments));
    }

    [HttpGet]
    public async Task<IActionResult> MyResults()
    {
        var response = await _api.GetMyResultsAsync();
        if (!response.Success)
        {
            TempData["Error"] = response.Error;
        }

        return View(response.Data ?? new List<ApiRaceResult>());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var response = await _api.GetProfileAsync();
        if (!response.Success || response.Data is null)
        {
            TempData["Error"] = response.Error ?? "We could not load your profile.";
            return RedirectToAction("Index", "Home");
        }

        return View(ToModel(response.Data));
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ParticipantProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new
        {
            firstName = model.FirstName.Trim(),
            lastName = model.LastName.Trim(),
            phone = model.Phone.Trim(),
            dateOfBirth = model.DateOfBirth!.Value.ToString("yyyy-MM-dd"),
            gender = model.Gender
        };

        var response = await _api.UpdateProfileAsync(request);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "We could not save your changes.");
            return View(model);
        }

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image to upload.";
            return RedirectToAction(nameof(Profile));
        }

        var response = await _api.UploadProfilePictureAsync(picture);
        if (response.Success)
        {
            TempData["Success"] = "Your profile picture has been updated.";
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return RedirectToAction(nameof(Profile));
    }

    private static ParticipantProfileViewModel ToModel(ApiProfile profile) => new()
    {
        FirstName = profile.FirstName,
        LastName = profile.LastName,
        Email = profile.Email,
        Phone = profile.Phone,
        DateOfBirth = profile.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
        Gender = profile.Gender,
        ProfilePicture = profile.ProfilePicture
    };
}
