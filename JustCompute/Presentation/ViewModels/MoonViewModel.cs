using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Entities.Models.Moon;
using Compute.Core.Domain.Services;
using JustCompute.Presentation.ViewModels.Base;
using static Compute.Core.Helpers.GroupingHelper;

namespace JustCompute.Presentation.ViewModels
{
    public partial class MoonViewModel : BaseViewModel
    {
        private readonly IMoonService _moonService;

        [ObservableProperty]
        List<Group<string, MoonCycle>> groupedMoonCycles;

        List<MoonCycle> moonCycleList = new();

        public MoonViewModel(IMoonService moonService)
        {
            _moonService = moonService;
            GroupedMoonCycles = new List<Group<string, MoonCycle>>();
            Commands.Add("ToggleGroupCommand", new Command<Group<string, MoonCycle>>(ToggleGroup));
        }

        protected override async Task GetData(double lat, double lng, int timeZoneOffset)
        {
            moonCycleList = await _moonService.GetMoonCyclesAsync(lat, lng, DateTime.Now, timeZoneOffset);

            GroupedMoonCycles = GetGroupedData(moonCycleList, item => item.GeoDate.Value.ToString("MMMM yyyy")).ToList();
        }

        private void ToggleGroup(Group<string, MoonCycle> group)
        {
            group.IsExpanded = !group.IsExpanded;

            var items = group.ToList();
            group.Clear();

            if (group.IsExpanded)
            {
                var groupKey = group.Key;
                group.InsertRange(
                    moonCycleList
                    .Where(x => x.GeoDate.Value
                    .ToString("MMMM yyyy") == groupKey)
                    .ToList());
            }
        }
    }
}
