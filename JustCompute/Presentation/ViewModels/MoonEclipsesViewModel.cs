using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Services.Moon;
using CoordinateSharp;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Presentation.ViewModels.Common;
using static Compute.Core.Helpers.GroupingHelper;

namespace JustCompute.Presentation.ViewModels
{
    public partial class MoonEclipsesViewModel : BaseViewModel, ICompute
    {
        private readonly IMoonService _moonService;

        [ObservableProperty]
        private List<Group<string, LunarEclipseDetails>> groupedEclipseList;

        List<LunarEclipseDetails> eclipseList =[];

        public MoonEclipsesViewModel(IMoonService moonService)
        {
            _moonService = moonService;
            GroupedEclipseList = [];
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