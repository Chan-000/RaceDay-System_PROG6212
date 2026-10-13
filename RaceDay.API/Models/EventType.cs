using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class EventType
    {
        [Key]
        public int EventTypeID { get; set; }

        [Required]
        [MaxLength(50)]
        public string TypeName { get; set; } = "";

        [MaxLength(255)]
        public string? Description { get; set; }

        //events that belong to this type
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
