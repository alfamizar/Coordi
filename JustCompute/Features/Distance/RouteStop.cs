using CommunityToolkit.Mvvm.ComponentModel;
using JustCompute.Shared.ViewModels;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.Distance
{
    /// <summary>
    /// One pin on the route, as the list shows it: its number, where it is, and the leg that
    /// leaves it for the next pin.
    ///
    /// The leg belongs to the row rather than to a separate list because that is how it reads —
    /// under the point it departs from — and the last pin simply has none.
    /// </summary>
    public partial class RouteStop : ObservableObject
    {
        [ObservableProperty]
        private int number;

        /// <summary>
        /// A city or place name when the pin came from the search or the device, and empty for a
        /// hand-typed position, where the coordinates are the whole story.
        /// </summary>
        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string coordinates = string.Empty;

        /// <summary>"↓ 343 km · 148°", or empty on the last pin.</summary>
        [ObservableProperty]
        private string leg = string.Empty;

        [ObservableProperty]
        private bool hasLeg;

        /// <summary>The editor for this pin's coordinates, shown when the row is expanded.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditing))]
        private bool isExpanded;

        public bool IsEditing => IsExpanded;

        /// <summary>
        /// The pin's position, in the observable form the coordinate editor binds to.
        /// </summary>
        public EditableLocation Editable { get; }

        /// <summary>
        /// The pin as the route maths sees it. Read from <see cref="Editable"/> every time rather
        /// than stored alongside it, so a coordinate typed into the editor is measured rather
        /// than a stale copy of what it used to be.
        /// </summary>
        public Location Location => Editable.ToLocation();

        public RouteStop(Location location) => Editable = new EditableLocation(location);
    }
}
