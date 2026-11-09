using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly IRaceDayApi _api;

    public AccountController(IRaceDayApi api)
    {
        _api = api;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool expired = false)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (expired)
        {
            TempData["Warning"] = "Your session has ended. Please log in again.";
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _api.LoginAsync(model.Email.Trim(), model.Password);
        if (!response.Success || response.Data is null)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "We could not log you in. Please try again.");
            return View(model);
        }

        await SignInAsync(response.Data);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == "Organiser")
        {
            if (string.IsNullOrWhiteSpace(model.OrganisationName))
            {
                ModelState.AddModelError(nameof(model.OrganisationName), "Please enter your organisation name.");
            }
        }
        else if (model.Role == "Participant")
        {
            if (model.DateOfBirth is null)
            {
                ModelState.AddModelError(nameof(model.DateOfBirth), "Please enter your date of birth.");
            }

            if (string.IsNullOrWhiteSpace(model.Gender))
            {
                ModelState.AddModelError(nameof(model.Gender), "Please choose your gender.");
            }
        }
        else
        {
            ModelState.AddModelError(nameof(model.Role), "Please choose how you will use RaceDay.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new
        {
            firstName = model.FirstName.Trim(),
            lastName = model.LastName.Trim(),
            email = model.Email.Trim(),
            password = model.Password,
            phone = model.Phone.Trim(),
            role = model.Role,
            organisationName = model.Role == "Organiser" ? model.OrganisationName?.Trim() : null,
            dateOfBirth = model.Role == "Participant" ? model.DateOfBirth?.ToString("yyyy-MM-dd") : null,
            gender = model.Role == "Participant" ? model.Gender : null
        };

        var registered = await _api.RegisterAsync(request);
        if (!registered.Success)
        {
            ModelState.AddModelError(string.Empty, registered.Error ?? "We could not create your account. Please try again.");
            return View(model);
        }

        var login = await _api.LoginAsync(model.Email.Trim(), model.Password);
        if (!login.Success || login.Data is null)
        {
            TempData["Success"] = "Your account has been created. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        await SignInAsync(login.Data);
        TempData["Success"] = "Welcome to RaceDay!";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInAsync(ApiAuthResponse auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.UserId.ToString()),
            new(ClaimTypes.Name, auth.FirstName),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Role, auth.Role),
            new("api_token", auth.Token)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = false,
            ExpiresUtc = new DateTimeOffset(DateTime.SpecifyKind(auth.ExpiresAtUtc, DateTimeKind.Utc))
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
    }
}
