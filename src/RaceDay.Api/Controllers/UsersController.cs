using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly IImageStorage _images;

    public UsersController(RaceDayDbContext db, IPasswordService passwords, IImageStorage images)
    {
        _db = db;
        _passwords = passwords;
        _images = images;
    }

    [HttpGet("me")]
    [SwaggerOperation(Summary = "Returns the profile of the logged-in user (Organiser or Participant).")]
    [SwaggerResponse(StatusCodes.Status200OK, "The user's profile.", typeof(UserProfileDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The account no longer exists.")]
    public async Task<IActionResult> GetMe()
    {
        var id = User.GetUserId();

        if (User.IsInRole(RoleNames.Organiser))
        {
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.OrganiserID == id);
            return organiser is null ? NotFound() : Ok(organiser.ToProfile());
        }

        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.ParticipantID == id);
        return participant is null ? NotFound() : Ok(participant.ToProfile());
    }

    [HttpPut("me")]
    [SwaggerOperation(
        Summary = "Updates the logged-in user's own profile.",
        Description = "Organisers may change names, phone and organisationName. Participants may change names, phone, dateOfBirth and gender.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The updated profile.", typeof(UserProfileDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation failed.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The account no longer exists.")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var id = User.GetUserId();

        if (User.IsInRole(RoleNames.Organiser))
        {
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.OrganiserID == id);
            if (organiser is null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.OrganisationName))
            {
                return Problem(detail: "Organisers must supply an organisationName.", statusCode: StatusCodes.Status400BadRequest);
            }

            organiser.FirstName = request.FirstName.Trim();
            organiser.LastName = request.LastName.Trim();
            organiser.Phone = request.Phone.Trim();
            organiser.OrganisationName = request.OrganisationName.Trim();
            await _db.SaveChangesAsync();
            return Ok(organiser.ToProfile());
        }

        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.ParticipantID == id);
        if (participant is null)
        {
            return NotFound();
        }

        if (request.DateOfBirth is null || string.IsNullOrWhiteSpace(request.Gender))
        {
            return Problem(detail: "Participants must supply dateOfBirth and gender.", statusCode: StatusCodes.Status400BadRequest);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dob = request.DateOfBirth.Value;
        if (dob >= today || dob < today.AddYears(-120))
        {
            return Problem(detail: "Date of birth must be a valid date in the past.", statusCode: StatusCodes.Status400BadRequest);
        }

        participant.FirstName = request.FirstName.Trim();
        participant.LastName = request.LastName.Trim();
        participant.Phone = request.Phone.Trim();
        participant.DateOfBirth = dob;
        participant.Gender = request.Gender;
        await _db.SaveChangesAsync();
        return Ok(participant.ToProfile());
    }

    [HttpPost("me/picture")]
    [Authorize(Roles = RoleNames.Participant)]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Uploads the logged-in Participant's profile picture to the API's wwwroot folder.",
        Description = "Send a multipart/form-data request with a single file field named 'file' (JPEG, PNG, WebP or GIF, maximum 5 MB). An existing picture is replaced.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The updated profile including the new profilePicture path.", typeof(UserProfileDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "No file, wrong file type, or file too large.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Logged in as an Organiser.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The account no longer exists.")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Image storage is unavailable.", typeof(ProblemDetails))]
    public async Task<IActionResult> UploadPicture([FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.ParticipantID == id);
        if (participant is null)
        {
            return NotFound();
        }

        var error = ImageRules.Validate(request.File, out var extension);
        if (error is not null)
        {
            return Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);
        }

        string blobName;
        try
        {
            var file = request.File!;
            await using var stream = file.OpenReadStream();
            blobName = await _images.UploadAsync("profiles", $"participant-{id}", stream, file.ContentType, extension, cancellationToken);
        }
        catch (Exception)
        {
            return Problem(detail: "Image storage is currently unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var previous = participant.ProfilePicture;
        participant.ProfilePicture = blobName;
        await _db.SaveChangesAsync();

        if (previous is not null)
        {
            try
            {
                await _images.DeleteAsync(previous, cancellationToken);
            }
            catch (Exception)
            {
            }
        }

        return Ok(participant.ToProfile());
    }

    [HttpPut("me/password")]
    [SwaggerOperation(Summary = "Changes the logged-in user's password after checking the current one.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Password changed.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Current password is wrong or the new password is invalid.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var id = User.GetUserId();

        if (User.IsInRole(RoleNames.Organiser))
        {
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.OrganiserID == id);
            if (organiser is null)
            {
                return Unauthorized();
            }

            if (!_passwords.Verify(organiser.PasswordHash, request.CurrentPassword))
            {
                return Problem(detail: "Current password is incorrect.", statusCode: StatusCodes.Status400BadRequest);
            }

            organiser.PasswordHash = _passwords.Hash(request.NewPassword);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.ParticipantID == id);
        if (participant is null)
        {
            return Unauthorized();
        }

        if (!_passwords.Verify(participant.PasswordHash, request.CurrentPassword))
        {
            return Problem(detail: "Current password is incorrect.", statusCode: StatusCodes.Status400BadRequest);
        }

        participant.PasswordHash = _passwords.Hash(request.NewPassword);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
