using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Repository
{
    /// <summary>
    /// Where the user's choice of location outlives the process.
    ///
    /// The choice itself is a domain fact — it decides what every screen computes — so the rules
    /// around it live in Compute.Core. Where it is written down is a platform detail, and stays
    /// behind this port.
    /// </summary>
    public interface ILocationSelectionStore
    {
        /// <summary>Id of the saved place last chosen, or null when the choice was not a saved place.</summary>
        int? SelectedLocationId { get; set; }

        /// <summary>Whether the user has ever chosen a location themselves.</summary>
        bool HasUserChosen { get; set; }

        /// <summary>
        /// Where the device was when the app last followed it, or null. Read back at launch so the
        /// screens draw for it straight away instead of waiting on a fresh fix.
        /// </summary>
        GeoPoint? LastDevicePosition { get; set; }
    }
}
