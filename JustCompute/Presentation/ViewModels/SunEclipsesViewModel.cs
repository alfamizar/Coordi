using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Services.Sun;
using CoordinateSharp;
using JustCompute.Presentation.ViewModels.Base;
using static Compute.Core.Helpers.GroupingHelper;

namespace JustCompute.Presentation.ViewModels
{
    public partial class SunEclipsesViewModel : BaseViewModel
    {
        private readonly ISunService _sunService;

        [ObservableProperty]
        private List<Group<string, SolarEclipseDetails>> groupedEclipseList = [];

        List<SolarEclipseDetails> eclipseList = [];

        public SunEclipsesViewModel(ISunService sunService)
        {
            _sunService = sunService;
            Commands.Add("ToggleGroupCommand", new Command<Group<string, SolarEclipseDetails>>(ToggleGroup));
        }

        protected override async Task GetData(double lat, double lng, int timeZoneOffset)
        {
            eclipseList = await _sunService.GetSunEclipsesAsync(lat, lng, DateTime.Now);

            GroupedEclipseList = GetGroupedData(eclipseList, item => item.Date.ToString("yyyy")).ToList();
        }

        private void ToggleGroup(Group<string, SolarEclipseDetails> group)
        {
            group.IsExpanded = !group.IsExpanded;

            var items = group.ToList();
            group.Clear();

            if (group.IsExpanded)
            {
                var groupKey = group.Key;
                group.InsertRange(
                    eclipseList
                    .Where(x => x.Date
                    .ToString("yyyy") == groupKey)
                    .ToList());
            }
        }
    }
}