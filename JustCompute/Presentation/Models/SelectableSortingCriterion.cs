using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Common.Sort;

namespace JustCompute.Presentation.Models
{
    public partial class SelectableSortingCriterion : ObservableObject
    {
        [ObservableProperty]
        private Sorting sortingCriterion;

        [ObservableProperty]
        private bool isSelected;
    }
}
