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
        public Users? Participant { get; set; }

        //Event being entered
        public Events? Event { get; set; }

        //selected category
        public Category? Category { get; set; }

        // result for this enrolment (optional)
        public Results? Result { get; set; }
    }
}
