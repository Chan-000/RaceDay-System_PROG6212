using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels
{
    public class EventEnrolmentsVm
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public List<EnrolmentDto> Enrolments { get; set; } = new();
    }
}


