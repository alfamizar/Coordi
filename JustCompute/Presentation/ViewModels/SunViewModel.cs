using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Services;
using JustCompute.Presentation.ViewModels.Base;
using static Compute.Core.Helpers.GroupingHelper;

namespace JustCompute.Presentation.ViewModels
{
    public partial class SunViewModel : BaseViewModel
    {
        private readonly ISunService _sunService;

        [ObservableProperty]
        List<Group<string, BaseCelestialBodyCycle>> groupedSunCycles;

        List<BaseCelestialBodyCycle> sunCycleList = new();

        public SunViewModel(ISunService sunService)
        {
            _sunService = sunService;
            GroupedSunCycles = new List<Group<string, BaseCelestialBodyCycle>>();
            Commands.Add("ToggleGroupCommand", new Command<Group<string, BaseCelestialBodyCycle>>(ToggleGroup));
        }

        protected override async Task GetData(double lat, double lng, int timeZoneOffset)
        {
            sunCycleList = await _sunService.GetSunCyclesAsync(lat, lng, DateTime.Now, timeZoneOffset);

            GroupedSunCycles = GetGroupedData(sunCycleList, item => item.GeoDate.Value.ToString("MMMM yyyy")).ToList();
        }

        private void ToggleGroup(Group<string, BaseCelestialBodyCycle> group)
        {
            group.IsExpanded = !group.IsExpanded;

            var items = group.ToList();
            group.Clear();

            if (group.IsExpanded)
            {
                var groupKey = group.Key;
                group.InsertRange(
                    sunCycleList
                    .Where(x => x.GeoDate.Value
                    .ToString("MMMM yyyy") == groupKey)
                    .ToList());
            }
        }
    }
}