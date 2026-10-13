using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
 
    public class Event
    {
        [Key]
        public int EventID { get; set; }

        [Required]
        [MaxLength(150)]
        public string EventName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        [MaxLength(180)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(6,2)")]
        public decimal DistanceKm { get; set; }

        [Required]
        public int MaxParticipants { get; set; }

        public int EventTypeID { get; set; }

        // relationship to EventType
        public int OrganiserID { get; set; }

        // foreign keys
        public EventType? EventType { get; set; }

        //Relationship to the user who organises the event
        [ForeignKey(nameof(OrganiserID))]
        public User? Organiser { get; set; }

        // categories belonging to this event
        public ICollection<Category> Categories { get; set; } = new List<Category> ();

        // Enrolments for this event
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();

    }
}
