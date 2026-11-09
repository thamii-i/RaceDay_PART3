using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/images")]
[AllowAnonymous]
public class ImagesController : ControllerBase
{
    private readonly IImageStorage _images;

    public ImagesController(IImageStorage images)
    {
        _images = images;
    }

    [HttpGet("{**path}")]
    [SwaggerOperation(
        Summary = "Streams a stored image (event banner or profile picture) from the API's wwwroot/uploads folder.",
        Description = "The path is the value returned in bannerImage or profilePicture, for example banners/event-1-abc.png.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The image file.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No image exists at this path.")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Image storage is unavailable.", typeof(ProblemDetails))]
    public async Task<IActionResult> GetImage(string path, CancellationToken cancellationToken)
    {
        var allowed = path.StartsWith("banners/", StringComparison.Ordinal) || path.StartsWith("profiles/", StringComparison.Ordinal);
        if (!allowed || path.Contains("..", StringComparison.Ordinal))
        {
            return NotFound();
        }

        StoredImage? image;
        try
        {
            image = await _images.DownloadAsync(path, cancellationToken);
        }
        catch (Exception)
        {
            return Problem(detail: "Image storage is currently unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (image is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public,max-age=3600";
        return File(image.Content, image.ContentType);
    }
}
