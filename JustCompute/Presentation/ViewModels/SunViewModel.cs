using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Services.Sun;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Presentation.ViewModels.Common;
using static Compute.Core.Helpers.GroupingHelper;

namespace JustCompute.Presentation.ViewModels
{
    public partial class SunViewModel : BaseViewModel, ICompute
    {
        private readonly ISunService _sunService;

        [ObservableProperty]
        private List<Group<string, BaseCelestialBodyCycle>> groupedSunCycles = [];

        List<BaseCelestialBodyCycle> sunCycleList = [];

        public SunViewModel(ISunService sunService)
        {
            _sunService = sunService;
            Commands.Add("ToggleGroupCommand", new Command<Group<string, BaseCelestialBodyCycle>>(ToggleGroup));
        }

        protected override async Task GetData(double lat, double lng, int timeZoneOffset)
        {
            sunCycleList = await _sunService.GetSunCyclesAsync(lat, lng, DateTime.Now);

            GroupedSunCycles = [.. GetGroupedData(sunCycleList, item => item.GeoDate.Value.ToString("MMMM yyyy"))];
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