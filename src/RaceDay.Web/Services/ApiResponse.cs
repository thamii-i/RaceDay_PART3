using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Services;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public int StatusCode { get; init; }
    public string? Error { get; init; }
}

public class SessionExpiredException : Exception
{
    public SessionExpiredException() : base("The session has expired.")
    {
    }
}

public class SessionExpiredFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not SessionExpiredException)
        {
            return;
        }

        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Result = new RedirectToActionResult("Login", "Account", new { expired = true });
        context.ExceptionHandled = true;
    }
}
