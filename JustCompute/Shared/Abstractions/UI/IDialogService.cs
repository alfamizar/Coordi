namespace JustCompute.Shared.Abstractions.UI
{
    public interface IDialogService
    {

        Task<DialogButton> DisplayAlert(string title, string message, string accept);

        Task<DialogButton> DisplayAlert(string title, string message, string accept, string? cancel);

        Task<DialogButton> DisplayAlert(string title, string message, string accept, string? cancel, string? neutral);
    }
}
