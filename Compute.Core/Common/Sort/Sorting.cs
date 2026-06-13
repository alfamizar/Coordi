using CommunityToolkit.Mvvm.ComponentModel;

namespace Compute.Core.Common.Sort
{
    public record Sorting(SortCriterion Criterion, string DisplayName, SortDirection Direction = SortDirection.Ascending)
    {
        public override string ToString() => $"Sorting by {Criterion} in {Direction} order";
    }
}
