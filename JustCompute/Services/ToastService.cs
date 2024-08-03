using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Compute.Core.UI;

namespace JustCompute.Presentation.Dialogs
{
    public class ToastService : IToastService
    {
        public async Task ShowToast(
            string text,
            Duration duration,
            CancellationTokenSource? cancellationTokenSource = null,
            int fontSize = 16)
        {
            var toastDuration = duration == Duration.Long ? ToastDuration.Long : ToastDuration.Short;
            var toast = Toast.Make(text, toastDuration, fontSize);
            await toast.Show(cancellationTokenSource?.Token ?? default);
        }
    }
}
