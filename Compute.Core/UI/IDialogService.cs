using Compute.Core.Domain.Errors;

namespace Compute.Core.UI
{
    public interface IDialogService
    {
        Task<string> DisplayActionSheet(string title, string cancel, string destruction, params string[] buttons);

        Task<string> DisplayAlert(string title, string message, string accept);

        Task<string> DisplayAlert(string title, string message, string accept, string cancel);

        Task<string> DisplayAlert(string title, string message, string accept, string cancel, string neutral);

        Task<string> DisplayAdvancedDialog(string title,
                                           string message,
                                           string positiveButtonText,
                                           string negativeButtonText,
                                           string neutralButtonText);

        Task DisplayFault(Fault fault, string title);
    }
}