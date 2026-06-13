using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Common.Sort;

namespace JustCompute.Shared.Models
{
    public partial class SelectableSortingCriterion : ObservableObject
    {
        [ObservableProperty]
        private Sorting sortingCriterion = null!;

        [ObservableProperty]
        private bool isSelected;
    }
}
