using Location = Compute.Core.Domain.Entities.Models.Location;

namespace Compute.Core.Domain.Services
{
    /// <summary>The one place every screen computes from.</summary>
    public interface ILocationSelection
    {
        /// <summary>
        /// Never null. Until the user picks somewhere this is the device's own fix when there is
        /// one, and a placeholder when there is not, so screens show real data instead of an error.
        /// </summary>
        Location SelectedLocation { get; }

        /// <summary>
        /// True while the app has nowhere real to compute from: nothing chosen and no device fix,
        /// so every screen is running on the placeholder.
        /// </summary>
        bool ShouldPromptForLocation { get; }

        /// <summary>Records a place the user chose. Choosing the placeholder clears the choice.</summary>
        void Select(Location location);

        /// <summary>Reads back the place chosen in an earlier session. Runs at most once per launch.</summary>
        Task RestorePersistedSelectionAsync();

        /// <summary>
        /// Asks the device where it is when nothing has been chosen and permission already exists.
        /// Never raises a permission prompt: that request belongs to the Locations screen.
        /// </summary>
        Task FillFromDeviceIfUnchosenAsync();

        /// <summary>
        /// Raised when the selection moved to the device's fix without the user doing anything,
        /// so a screen already showing data knows it is now showing the wrong place.
        /// Held weakly: subscribe with a method, not a lambda.
        /// </summary>
        event EventHandler<EventArgs> DeviceFixAdopted;
    }
}
