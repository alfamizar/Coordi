using Compute.Core.Common.Sort;

namespace JustCompute.Shared.Popups;

public sealed record SortOptionsPopupResult(
    Sorting SelectedSortCriterion,
    IReadOnlyList<Sorting> SortingCriteria);
