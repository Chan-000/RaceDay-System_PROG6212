using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        public int EventID { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public int? AgeMin { get; set; }

        public int? AgeMax { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal EntryFee { get; set; } = 0;

        // Event to which the category belongs
        public Event? Event {  get; set; }

        //Enrolments using this category
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
