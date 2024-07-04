using CommunityToolkit.Maui.Views;
using Compute.Core.Domain.Errors;
using Compute.Core.UI;
using JustCompute.Presentation.Pages.Dialogs;

namespace JustCompute.Presentation.Dialogs
{
    public class DialogService : IDialogService
    {
        public Task<string> DisplayActionSheet(string title, string cancel, string destruction, params string[] buttons)
        {
            throw new NotImplementedException();
        }

        public async Task<string> DisplayAlert(string title, string message, string accept)
        {
            return await DisplayAlert(title, message, accept, null);
        }

        public async Task<string> DisplayAlert(string title, string message, string accept, string cancel)
        {
            return await DisplayAlert(title, message, accept, cancel, null);
        }

        public async Task<string> DisplayAlert(string title, string message, string accept, string cancel, string neutral)
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

            return (string)await Application.Current.MainPage.ShowPopupAsync(popup);
        }

        public Task<string> DisplayAdvancedDialog(string title, string message, string positiveButtonText, string negativeButtonText, string neutralButtonText)
        {
            throw new NotImplementedException();
        }

        public Task DisplayFault(Fault fault, string title)
        {
            throw new NotImplementedException();
        }
    }
}
