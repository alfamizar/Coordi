using Compute.Core.Domain.Errors;

namespace JustCompute.Shared.Abstractions.UI
{
    public interface IToastService
    {
        Task ShowToast(
            string text,
            Duration duration = Duration.Short,
            CancellationTokenSource? cancellationTokenSource = null,
            int fontSize = 16
            );
    }
}