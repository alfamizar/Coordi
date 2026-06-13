using Compute.Core.Domain.Errors;

namespace Compute.Core.UI
{
    public interface IDialogService
    {
        Task<T?> DisplayActionSheet<T>(
            string title,
            string cancel,
            string? destruction,
            params DialogAction<T>[] actions) where T : struct;

        Task<DialogButton> DisplayAlert(string title, string message, string accept);

        Task<DialogButton> DisplayAlert(string title, string message, string accept, string? cancel);

        Task<DialogButton> DisplayAlert(string title, string message, string accept, string? cancel, string? neutral);

        Task<DialogButton> DisplayAdvancedDialog(string title,
                                           string message,
                                           string positiveButtonText,
                                           string negativeButtonText,
                                           string neutralButtonText);

        Task DisplayFault(Fault fault, string title);
    }
}
