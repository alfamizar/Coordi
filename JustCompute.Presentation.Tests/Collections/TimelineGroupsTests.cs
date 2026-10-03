using System.ComponentModel;
using JustCompute.Presentation.Collections;
using static JustCompute.Presentation.Collections.GroupingHelper;

namespace JustCompute.Presentation.Tests.Collections
{
    public class TimelineGroupsTests
    {
        private static readonly DateTime Today = new(2026, 10, 3);

        [Fact]
        public void YesterdayIsPast() =>
            Assert.Equal(TimelineGroups.PastKey, TimelineGroups.KeyOf(Today.AddDays(-1), Today));

        [Fact]
        public void TodayIsNotPast_ItsTheOneWorthShowing() =>
            Assert.Equal("2026", TimelineGroups.KeyOf(Today, Today));

        [Fact]
        public void LaterTodayIsStillToday() =>
            Assert.Equal("2026", TimelineGroups.KeyOf(Today.AddHours(21), Today.AddHours(8)));

        [Fact]
        public void TheFutureIsGroupedByItsOwnYear() =>
            Assert.Equal("2027", TimelineGroups.KeyOf(new DateTime(2027, 8, 2), Today));

        [Fact]
        public void EarlierThisYearIsPast_NotThisYearsGroup() =>
            Assert.Equal(TimelineGroups.PastKey, TimelineGroups.KeyOf(new DateTime(2026, 8, 12), Today));

        [Fact]
        public void APastAndYearsTimelineKeepsItsOrder()
        {
            DateTime[] dates = [new(2001, 6, 21), new(2026, 8, 12), new(2026, 10, 3), new(2027, 8, 2)];

            var keys = GetGroupedData(dates, d => TimelineGroups.KeyOf(d, Today)).Select(g => g.Key);

            Assert.Equal([TimelineGroups.PastKey, "2026", "2027"], keys);
        }
    }

    public class GroupHeaderTests
    {
        [Fact]
        public void TheTitleIsTheKeyUntilTheGroupIsNamed()
        {
            var group = new Group<string, int>("2027", [1, 2]);
            Assert.Equal("2027", group.Title);

            group.Title = "Past eclipses (2)";
            Assert.Equal("Past eclipses (2)", group.Title);
            Assert.Equal("2027", ((IGroupHeader)group).Key);
        }

        [Fact]
        public void OpeningOrClosingIsAnnounced_SoTheMarkerFollows()
        {
            var group = new Group<string, int>("past", [1], isExpanded: false);
            var changed = new List<string?>();
            ((INotifyPropertyChanged)group).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            group.IsExpanded = true;
            group.IsExpanded = true;

            Assert.Equal([nameof(IGroupHeader.IsExpanded)], changed);
        }
    }
}
