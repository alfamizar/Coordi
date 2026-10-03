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
        /// Asks the device where it is, once per launch, when the selection follows it — nothing
        /// chosen, or the device's own row chosen — and permission already exists. Never raises a
        /// permission prompt: that request belongs to the Locations screen.
        ///
        /// A fix in the same town is taken silently. One that has carried the device to another
        /// town than the one the launch opened on is held back and offered through
        /// <see cref="CityChangeProposed"/> instead.
        /// </summary>
        Task RefreshFromDeviceAsync();

        /// <summary>
        /// For the app coming back to the foreground. A process Android kept alive can return hours
        /// and a train ride later, which is a launch to whoever is holding the phone, so after a
        /// while away the device is asked again — against the town on screen.
        /// </summary>
        Task RefreshOnReturnAsync();

        /// <summary>
        /// Raised when a fresh fix puts the device in another town than the one on screen. Nothing
        /// has moved: the selection waits for <see cref="AcceptProposedCityChange"/> or
        /// <see cref="DeclineProposedCityChange"/>. Held weakly: subscribe with a method, not a lambda.
        /// </summary>
        event EventHandler<CityChangeProposedEventArgs> CityChangeProposed;

        /// <summary>Moves to the proposed town: it becomes the device's position, and the one kept for next time.</summary>
        void AcceptProposedCityChange();

        /// <summary>
        /// Stays on the town on screen until the next launch, which asks again: the new position is
        /// neither announced nor kept.
        /// </summary>
        void DeclineProposedCityChange();

        /// <summary>
        /// Raised when the selection moved to the device's fix without the user doing anything,
        /// so a screen already showing data knows it is now showing the wrong place.
        /// Held weakly: subscribe with a method, not a lambda.
        /// </summary>
        event EventHandler<EventArgs> DeviceFixAdopted;
    }

    /// <param name="From">The town on screen.</param>
    /// <param name="To">The town the device is in now.</param>
    public sealed class CityChangeProposedEventArgs(Location from, Location to) : EventArgs
    {
        public Location From { get; } = from;

        public Location To { get; } = to;
    }
}
