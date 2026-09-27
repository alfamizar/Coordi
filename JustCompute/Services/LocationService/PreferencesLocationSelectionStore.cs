using Compute.Core.Repository;

namespace JustCompute.Services.LocationService
{
    /// <summary>
    /// The user's choice of location, in platform preferences.
    ///
    /// Both keys are the ones earlier versions wrote — "selected_location_id" from the GPS
    /// service and "HasUserSetLocation" from the static Settings class — so an update reads back
    /// the place a user had already chosen. Renaming either would quietly forget it for everyone.
    /// </summary>
    public sealed class PreferencesLocationSelectionStore : ILocationSelectionStore
    {
        private const string SelectedLocationIdKey = "selected_location_id";
        private const string HasUserChosenKey = "HasUserSetLocation";

        public int? SelectedLocationId
        {
            get
            {
                var id = Preferences.Default.Get(SelectedLocationIdKey, -1);
                return id > 0 ? id : null;
            }
            set
            {
                if (value is int id && id > 0)
                {
                    Preferences.Default.Set(SelectedLocationIdKey, id);
                }
                else
                {
                    Preferences.Default.Remove(SelectedLocationIdKey);
                }
            }
        }

        public bool HasUserChosen
        {
            get => Preferences.Default.Get(HasUserChosenKey, false);
            set => Preferences.Default.Set(HasUserChosenKey, value);
        }
    }
}
