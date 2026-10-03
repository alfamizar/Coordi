using System.Globalization;
using Compute.Core.Domain.Services;
using JustCompute.Resources.Strings;
using JustCompute.Shared.Abstractions.UI;
using JustCompute.Shared.Helpers;
using Microsoft.Extensions.Localization;

namespace JustCompute.Services.LocationService
{
    /// <summary>
    /// Asks the question the selection raises when a fresh fix lands in another town than the one
    /// on screen — "It looks like you're in Lyon now. Switch from Paris?" — the way Jakdojade asks
    /// before changing city. The answer goes back to the selection, which moves or stays.
    ///
    /// The selection decides when to ask and what each answer means; this only puts the question
    /// on screen, which is why it lives in the app and the rules live in Compute.Core.
    /// </summary>
    public sealed class CityChangePrompt(
        ILocationSelection selection,
        IDialogService dialogs,
        IStringLocalizer<AppStringsRes> localizer)
    {
        private readonly ILocationSelection _selection = selection;
        private readonly IDialogService _dialogs = dialogs;
        private readonly IStringLocalizer<AppStringsRes> _localizer = localizer;

        /// <summary>
        /// Starts listening. Called once at startup, before the first fix can be offered. A method
        /// group: the selection holds its subscribers weakly, and this one lives as long as the
        /// container that made it.
        /// </summary>
        public void Start() => _selection.CityChangeProposed += OnCityChangeProposed;

        private void OnCityChangeProposed(object? sender, CityChangeProposedEventArgs e) =>
            // Offered from whichever thread the fix arrived on; a dialog belongs on the UI thread.
            MainThread.BeginInvokeOnMainThread(() => AskAsync(e).Forget(nameof(CityChangePrompt)));

        private async Task AskAsync(CityChangeProposedEventArgs e)
        {
            bool switchTown = false;

            try
            {
                var answer = await _dialogs.DisplayAlert(
                    _localizer["CityChangeTitle"],
                    Format("CityChangeMessage", e.To.Name, e.From.Name),
                    Format("CityChangeSwitch", e.To.Name),
                    Format("CityChangeKeep", e.From.Name));

                switchTown = answer == DialogButton.Positive;
            }
            finally
            {
                // Answered either way, even when the dialog could not be shown: that counts as a
                // "not now", so the town on screen stays and the next launch asks again.
                if (switchTown)
                {
                    _selection.AcceptProposedCityChange();
                }
                else
                {
                    _selection.DeclineProposedCityChange();
                }
            }
        }

        private string Format(string key, params object[] args) =>
            string.Format(CultureInfo.CurrentCulture, _localizer[key], args);
    }
}
