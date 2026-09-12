using CommunityToolkit.Mvvm.ComponentModel;

namespace JustCompute.Features.Planets
{
    /// <summary>
    /// One planet as the screen shows it: a localized name and figures already formatted.
    ///
    /// Formatted here rather than in the XAML because the rows are not uniform — a planet that
    /// never sets has one line where the others have three, and the altitude at its highest
    /// belongs on the same line as the time it gets there. Doing that with converters would take
    /// four of them and still read worse than this.
    /// </summary>
    public partial class PlanetRow : ObservableObject
    {
        [ObservableProperty]
        private string name = string.Empty;

        /// <summary>Altitude above the horizon now, e.g. "12°" or "−34°".</summary>
        [ObservableProperty]
        private string altitude = string.Empty;

        /// <summary>Azimuth now, degrees clockwise from north.</summary>
        [ObservableProperty]
        private string azimuth = string.Empty;

        [ObservableProperty]
        private string rise = string.Empty;

        /// <summary>The transit time and the altitude it reaches, e.g. "12:37 · 28°".</summary>
        [ObservableProperty]
        private string highest = string.Empty;

        [ObservableProperty]
        private string set = string.Empty;

        [ObservableProperty]
        private string distance = string.Empty;

        /// <summary>
        /// Shown in place of the rise and set rows when there are none: above the horizon all
        /// day, or below it all day. Empty on an ordinary day, which is what hides the line.
        /// </summary>
        [ObservableProperty]
        private string allDayNote = string.Empty;

        [ObservableProperty]
        private bool hasRiseAndSet;

        /// <summary>Whether the planet is up now, which is the one thing a reader looks for first.</summary>
        [ObservableProperty]
        private bool isUp;
    }
}
