using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Compute.Core.Domain.Errors;
using Compute.Core.UI;
using JustCompute.Shared.Popups;

namespace JustCompute.Services
{
    public class DialogService : IDialogService
    {
        public async Task<T?> DisplayActionSheet<T>(
            string title,
            string cancel,
            string? destruction,
            params DialogAction<T>[] actions) where T : struct
        {
            var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page
#pragma warning disable CS0618 // MainPage is deprecated; kept only as a fallback when no window page is available
                ?? Application.Current?.MainPage;
#pragma warning restore CS0618

            if (currentPage is null)
            {
                return null;
            }

            var destroyButton = string.IsNullOrWhiteSpace(destruction) ? null : destruction;
            var selectedText = await currentPage.DisplayActionSheetAsync(
                title,
                cancel,
                destroyButton,
                actions.Select(action => action.Text).ToArray());

            var selectedAction = actions.FirstOrDefault(action => action.Text == selectedText);
            return selectedAction is null ? null : selectedAction.Value;
        }

        public async Task<DialogButton> DisplayAlert(string title, string message, string accept)
        {
            return await DisplayAlert(title, message, accept, null);
        }

        public async Task<DialogButton> DisplayAlert(string title, string message, string accept, string? cancel)
        {
            return await DisplayAlert(title, message, accept, cancel, null);
        }

        public async Task<DialogButton> DisplayAlert(string title, string message, string accept, string? cancel, string? neutral)
        {
            NoOutsideTapDismissPopup popup = new()
            {
                DialogTitle = title,
                DialogMessage = message,
                NegativeButtonText = cancel,
                PositiveButtonText = accept,
                NeutralButtonText = neutral,
                CanBeDismissedByTappingOutsideOfPopup = false
            };

            var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page
#pragma warning disable CS0618 // MainPage is deprecated; kept only as a fallback when no window page is available
                ?? Application.Current?.MainPage;
#pragma warning restore CS0618

            if (currentPage is null)
            {
                return DialogButton.None;
            }

            var popupOptions = new PopupOptions
            {
                PageOverlayColor = Color.FromArgb("#66000000"),
                Shadow = null,
                Shape = null,
                CanBeDismissedByTappingOutsideOfPopup = false
            };

            var popupResult = await currentPage.ShowPopupAsync<DialogResult>(popup, popupOptions, CancellationToken.None);

            return popupResult.Result?.Button ?? popup.SelectedButton;
        }

        public Task<DialogButton> DisplayAdvancedDialog(string title, string message, string positiveButtonText, string negativeButtonText, string neutralButtonText)
        {
            throw new NotImplementedException();
        }

        public Task DisplayFault(Fault fault, string title)
        {
            throw new NotImplementedException();
        }
    }
}
