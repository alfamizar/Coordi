using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using JustCompute.Shared.Abstractions.UI;

namespace JustCompute.Shared.Popups
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
