using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels
{
    public class CaptureResultVm
    {
        public int EnrolmentID { get; set; }

        [Required]
        [Display(Name = "Finish Time (hh:mm:ss)")]
        public TimeSpan FinishTime { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage ="Finishing position must be atleast 1.")]
        [Display(Name = "Finishing Position")]
        public int FinishPosition { get; set; }

        public string ParticipantName { get; set; } = string.Empty;
        public string EventName { get; set;  } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

    }
}
