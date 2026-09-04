using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Shared.ViewModels
{
    /// <summary>
    /// What the Add/Edit Location screen is being opened for.
    ///
    /// This used to travel as a single-entry <c>Dictionary&lt;LocationInputContext, Location&gt;</c>
    /// that the receiver unpacked with <c>FirstOrDefault()</c> — a dictionary standing in for a
    /// pair, where nothing said the pair was the whole of it. It also only type-checked on arrival
    /// by luck: two of the three senders built theirs as <c>Location?</c>, which matches only
    /// because nullable annotations are erased at runtime.
    /// </summary>
    /// <param name="Location">
    /// The place to edit, or null when adding one from scratch.
    /// </param>
    public sealed record LocationEditorArgs(LocationInputContext Context, Location? Location);
}
