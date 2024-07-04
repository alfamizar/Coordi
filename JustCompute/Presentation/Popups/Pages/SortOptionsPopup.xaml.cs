using CommunityToolkit.Maui.Views;
using Compute.Core.Common.Sort;
using JustCompute.Presentation.Models;
using JustCompute.Presentation.Popups.ViewModels;

namespace JustCompute.Presentation.Popups.Pages;

public partial class SortOptionsPopup : Popup
{
    private readonly SortOptionsPopupViewModel viewModel;

    public SortOptionsPopup(SortOptionsPopupViewModel vm)
    {
        InitializeComponent();
        viewModel = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = viewModel;

        viewModel.AnchorChanged += OnAnchorChanged;
    }

    private async void OnItemTapped(object sender, EventArgs e)
    {
        if (e is TappedEventArgs { Parameter: SelectableSortingCriterion tappedItem })
        {
            var viewModel = BindingContext as SortOptionsPopupViewModel;
            viewModel?.Commands["SelectSortCriterionCommand"].Execute(tappedItem);

            var selectedSortCriterion = tappedItem.SortingCriterion;
            List<Sorting>? criteria = viewModel?.SortingCriteria.Select(x => x.SortingCriterion).ToList();

            var result = new Tuple<Sorting, List<Sorting>?>(selectedSortCriterion, criteria);

            await CloseAsync(result);
        }
    }

    /*private void OnItemTapped(SelectableSortingCriterion selectedItem)
    {
        var viewModel = BindingContext as SortOptionsPopupViewModel;
        viewModel?.Commands["SelectSortCriterionCommand"].Execute(selectedItem);
        CloseAsync(selectedItem);
    }*/

    private void OnAnchorChanged(object? sender, EventArgs e)
    {
        if (viewModel.Anchor != null)
        {
            Anchor = viewModel.Anchor;
        }
    }

    protected override Task OnDismissedByTappingOutsideOfPopup(CancellationToken token = default)
    {
        CleanUp();
        return base.OnDismissedByTappingOutsideOfPopup(token);
    }

    protected override Task OnClosed(object? result, bool wasDismissedByTappingOutsideOfPopup, CancellationToken token = default)
    {
        CleanUp();
        return base.OnClosed(result, wasDismissedByTappingOutsideOfPopup, token);
    }

    private void CleanUp()
    {
        if (viewModel != null)
        {
            viewModel.AnchorChanged -= OnAnchorChanged;
        }
    }
}
