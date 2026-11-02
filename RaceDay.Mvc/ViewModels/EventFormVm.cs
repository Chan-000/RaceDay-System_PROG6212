using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels
{
    public class EventFormVm
    {
        public int EventID { get; set; }

        [Required]
        [MaxLength(150)]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [MaxLength(180)]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "999.99",
            ErrorMessage = "Distannce must be greater than zero")]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Maximum participants must be at least 1.")]
        [Display(Name = "Maximum Participants")]
        public int MaxParticipants { get; set; } = 100;

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select an event type.")]
        [Display(Name = "Event Type")]
        public int EventTypeID { get; set; }

        public List<EventTypeOptionVm> EventTypes { get; set; } = new();

    }
    public class EventTypeOptionVm
    {
        public int EventTypeID { get; set; }

        public string TypeName { get; set; } = string.Empty;
    }
}