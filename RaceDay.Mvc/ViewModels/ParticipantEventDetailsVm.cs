using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels
{
    public class ParticipantEventDetailsVm
    {
        public EventDto Event { get; set; } = new EventDto();

        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();

        public int SelectedCategoryID { get; set; }
    }
}
