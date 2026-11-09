using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class MediaController : Controller
{
    private readonly IRaceDayApi _api;

    public MediaController(IRaceDayApi api)
    {
        _api = api;
    }

    [HttpGet("/media/{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        var image = await _api.GetImageAsync(path);
        if (image is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public,max-age=3600";
        return File(image.Value.Bytes, image.Value.ContentType);
    }
}
