using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

//authentication DTOs

namespace RaceDay.Contracts
{
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required,MinLength(8)]
        public string Password { get; set; } = string.Empty;
        [Required]
        [RegularExpression("^(Organiser|Participant)$")]
        public string Role { get; set; } = "Participant";
        public string? PhoneNumber { get; set; }
    }

    public class AuthResponse
    {
        public int UserID { get; set; } 
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    //Profile DTOs
    public class ProfileDto
    {
        public int UserID { get; set; } 
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class UpdateProfileRequest
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
    }

    //Event DTOs
    public class CreateEventRequest
    {
        [Required, MaxLength(150)]
        public string EventName { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        [Required]
        public DateTime EventDate { get; set; }
        [Required, MaxLength(180)]
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        [Range(1, int.MaxValue)]
        public int MaxParticipants { get; set; }
        [Range(1, int.MaxValue)]
        public int EventTypeID { get; set; }
    }

    public class UpdateEventRequest : CreateEventRequest
    {
    }

    public class EventDto
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public int MaxParticipants { get; set; }
        public int EventTypeID { get; set; }
        public int OrganiserID { get; set; }
    }

    //Category DTOs
    public class CreateCategoryRequest
    {
        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;
        public int? AgeMin { get; set; }
        public int? AgeMax { get; set; }
        [Range(typeof(decimal), "0", "99999999")]
        public decimal EntryFee { get; set; }
    }

    public class CategoryDto
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? AgeMin { get; set; }
        public int? AgeMax { get; set; }
        public decimal EntryFee { get; set; }
    }

    //Enrolment DTOs
    public class CreateEnrolmentRequest
    {
        [Range(1, int.MaxValue)]
        public int CategoryID { get; set; }
    }

    public class UpdateEnrolmentStatusRequest
    {
        [Required]
        [RegularExpression("^(Confirmed|Cancelled)$")]
        public string EnrolmentStatus { get; set; } = "Confirmed";
    }

    public class EnrolmentDto
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public int EventID { get; set; }
        public int CategoryID { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string EnrolmentStatus { get; set; } = string.Empty;
    }

    //Result
    public class CreateResultRequest
    {
        [Range(1, int.MaxValue)]
        public int EnrolmentID { get; set; }
        public TimeSpan? FinishTime { get; set; }
        [Range(1, int.MaxValue)]
        public int? FinishPosition { get; set; }  
    }

    public class ResultDto
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? FinishPosition { get; set; }
    }

    // Part 3 image-upoad Dto
    public class UploadResponse
    {
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

}
