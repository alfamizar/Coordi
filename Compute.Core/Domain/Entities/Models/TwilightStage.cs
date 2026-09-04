using Compute.Astro;

namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>
    /// One twilight boundary that actually occurs on the day being shown, in the location's local
    /// time. Stages the Sun never reaches are absent from the list rather than present and empty,
    /// so a white night simply has fewer rows.
    /// </summary>
    public record TwilightStage(TwilightLabel Label, DateTime Time);
}
