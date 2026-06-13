using CommunityToolkit.Maui.Views;
using JustCompute.Shared.Models;
using JustCompute.Shared.Popups;

namespace JustCompute.Shared.Popups;

public partial class SortOptionsPopup : Popup<SortOptionsPopupResult>
{
    private readonly SortOptionsPopupViewModel viewModel;

    public SortOptionsPopup(SortOptionsPopupViewModel vm)
    {
        InitializeComponent();
        viewModel = vm ?? throw new ArgumentNullException(nameof(vm));
        BindingContext = viewModel;
        Closed += OnClosed;
    }

    private async void OnItemTapped(object sender, EventArgs e)
    {
        if (e is TappedEventArgs { Parameter: SelectableSortingCriterion tappedItem })
        {
            var viewModel = BindingContext as SortOptionsPopupViewModel;
            viewModel?.Commands["SelectSortCriterionCommand"].Execute(tappedItem);

            if (viewModel?.SelectedSortCriterion is null)
            {
                await CloseAsync();
                return;
            }

            await CloseAsync(new SortOptionsPopupResult(
                viewModel.SelectedSortCriterion.SortingCriterion,
                viewModel.SortingCriteria.Select(x => x.SortingCriterion).ToList()));
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        CleanUp();
    }

    private void CleanUp()
    {
        Closed -= OnClosed;
    }
}
