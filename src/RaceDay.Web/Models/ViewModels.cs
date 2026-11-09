using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
    public string Role { get; set; } = "Participant";

    [Required(ErrorMessage = "Please enter your first name.")]
    [StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your phone number.")]
    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Organisation name")]
    public string? OrganisationName { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }
}

public class EventFilterViewModel
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string Scope { get; set; } = "upcoming";
    public List<ApiEvent> Events { get; set; } = new();
}

public class EventDetailsViewModel
{
    public ApiEvent Event { get; set; } = new();
    public List<ApiCategory> Categories { get; set; } = new();
    public ApiEnrollment? MyEnrollment { get; set; }
    public bool IsPast { get; set; }
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(100)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a description.")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance in kilometres.")]
    [Range(0.1, 999, ErrorMessage = "The distance must be between 0.1 and 999 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose the event type.")]
    [Display(Name = "Event type")]
    public string? EventType { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }

    public string? ExistingBanner { get; set; }
}

public class CategoriesPageViewModel
{
    public ApiEvent Event { get; set; } = new();
    public List<ApiCategory> Categories { get; set; } = new();
    public List<ApiCategory> AllCategories { get; set; } = new();
}

public class EnrollmentsPageViewModel
{
    public ApiEvent Event { get; set; } = new();
    public List<ApiEnrollment> Enrollments { get; set; } = new();
}

public class ResultRow
{
    public ApiEnrollment Enrollment { get; set; } = new();
    public ApiRaceResult? Result { get; set; }
}

public class ResultsPageViewModel
{
    public ApiEvent Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
    public bool IsPast { get; set; }
}

public class DashboardViewModel
{
    public List<ApiEvent> Events { get; set; } = new();
    public List<ApiEvent> Upcoming { get; set; } = new();
    public int TotalEnrollments { get; set; }
}

public class ParticipantProfileViewModel
{
    [Required(ErrorMessage = "Please enter your first name.")]
    [StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your phone number.")]
    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your date of birth.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Please choose your gender.")]
    public string? Gender { get; set; }

    public string? ProfilePicture { get; set; }
}

public class OrganiserProfileViewModel
{
    [Required(ErrorMessage = "Please enter your first name.")]
    [StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your phone number.")]
    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your organisation name.")]
    [StringLength(100)]
    [Display(Name = "Organisation name")]
    public string OrganisationName { get; set; } = string.Empty;
}

public class ErrorViewModel
{
    public int Code { get; set; }
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "Please try again in a moment.";
}
