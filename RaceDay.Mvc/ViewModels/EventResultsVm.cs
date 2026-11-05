using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels
{
    public class EventResultsVm
    {
        public int EventID { get; set; }

        public string EventName { get; set; } = string.Empty;
        public List<ResultDto> Results { get; set; } = new();
    }
}
