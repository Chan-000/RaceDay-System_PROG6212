using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        public int ParticipantID { get; set; }

        public int EventID { get; set; }

        public int CategoryID { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime EnrolmentDate { get; set; } = DateTime.Today;

        [Required]
        [MaxLength(20)]
        public string EnrolmentStatus { get; set; } = "confirmed";

        // user who registered
        [ForeignKey(nameof(ParticipantID))]
        public User? Participant { get; set; }

        //Event being entered
        public Event? Event { get; set; }

        //selected category
        public Category? Category { get; set; }

        // result for this enrolment (optional)
        public Result? Result { get; set; }
    }
}
