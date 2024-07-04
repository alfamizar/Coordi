using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Common.Sort;
using JustCompute.Presentation.Models;
using JustCompute.Presentation.ViewModels.Base;
using System.Collections.ObjectModel;

namespace JustCompute.Presentation.Popups.ViewModels
{
    public partial class SortOptionsPopupViewModel : BaseViewModel
    {
        private readonly WeakEventManager eventManager = new();

        [ObservableProperty]
        public View? anchor;

        [ObservableProperty]
        private ObservableCollection<SelectableSortingCriterion> sortingCriteria;

        [ObservableProperty]
        private SelectableSortingCriterion selectedSortCriterion;

        public SortOptionsPopupViewModel()
        {
            sortingCriteria = [];
            Commands.Add("SelectSortCriterionCommand", new Command<SelectableSortingCriterion>(OnSelectSortCriterion));
        }

        partial void OnAnchorChanged(View? value)
        {
            eventManager.HandleEvent(this, EventArgs.Empty, nameof(AnchorChanged));
        }

        public event EventHandler<EventArgs> AnchorChanged
        {
            add => eventManager.AddEventHandler(value);
            remove => eventManager.RemoveEventHandler(value);
        }

        public void ApplyParameters(List<Sorting> criteria, Sorting selectedCriterion, View? anchor = null)
        {
            SortingCriteria = new ObservableCollection<SelectableSortingCriterion>(criteria.Select(c => new SelectableSortingCriterion
            {
                SortingCriterion = c,
                IsSelected = c.Criterion == selectedCriterion.Criterion
            }).ToList());

            SelectedSortCriterion = SortingCriteria.First(c => c.SortingCriterion.Criterion == selectedCriterion.Criterion);
            Anchor = anchor;
        }

        private void OnSelectSortCriterion(SelectableSortingCriterion selected)
        {
            if (SelectedSortCriterion == selected)
            {
                selected.SortingCriterion = selected.SortingCriterion with
                {
                    Direction = selected.SortingCriterion.Direction == SortDirection.Ascending
                    ? SortDirection.Descending
                    : SortDirection.Ascending
                };
            }
            else
            {
                // Deselect all criteria
                foreach (var criterion in SortingCriteria)
                {
                    criterion.IsSelected = false;
                }

                // Mark the newly selected criterion
                selected.IsSelected = true;
                SelectedSortCriterion = selected;
            }

            var criteria = SortingCriteria.ToList();
            SortingCriteria = new ObservableCollection<SelectableSortingCriterion>(criteria);
        }
    }
}
