using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly IRaceDayApi _api;

    public HomeController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(EventFilterViewModel filter)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View("Landing");
        }

        if (User.IsInRole("Organiser"))
        {
            return RedirectToAction("Dashboard", "Organiser");
        }

        var response = await _api.GetEventsAsync(filter.Type, filter.Scope != "all", filter.Search);
        if (response.Success)
        {
            filter.Events = response.Data ?? new List<ApiEvent>();
        }
        else
        {
            TempData["Error"] = response.Error;
        }

        return View(filter);
    }

    [HttpGet]
    public IActionResult Error(int? code)
    {
        var model = new ErrorViewModel { Code = code ?? 500 };

        if (code == 404)
        {
            model.Title = "Page not found";
            model.Message = "The page you were looking for does not exist.";
        }
        else if (code == 403)
        {
            model.Title = "Access denied";
            model.Message = "You do not have permission to view that page.";
        }

        Response.StatusCode = model.Code;
        return View(model);
    }
}
