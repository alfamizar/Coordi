using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Services;
using Compute.Core.Helpers;
using CoordinateSharp;
using JustCompute.Presentation.ViewModels.Base;
using static Compute.Core.Helpers.GroupingHelper;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels
{
    public partial class MoonEclipsesViewModel : BaseViewModel
    {
        private readonly IMoonService _moonService;

        [ObservableProperty]
        List<Group<string, LunarEclipseDetails>> groupedEclipseList;

        List<LunarEclipseDetails> eclipseList = new();

        public MoonEclipsesViewModel(IMoonService moonService)
        {
            _moonService = moonService;
            GroupedEclipseList = new List<Group<string, LunarEclipseDetails>>();
            Commands.Add("ToggleGroupCommand", new Command<Group<string, LunarEclipseDetails>>(ToggleGroup));
        }

        protected override async Task GetData(double lat, double lng, int timeZoneOffset)
        {
            eclipseList = await _moonService.GetMoonEclipsesAsync(lat, lng, DateTime.Now);

            GroupedEclipseList = GetGroupedData(eclipseList, item => item.Date.ToString("yyyy")).ToList();
        }

        private void ToggleGroup(Group<string, LunarEclipseDetails> group)
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