namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>
    /// A plain domain record. It deliberately raises no change notifications: the domain layer
    /// has no opinion about how a screen redraws, and the editing UI works on its own observable
    /// copy rather than binding straight at this.
    /// </summary>
    public class City
    {
        /// <summary>
        /// Row id of this city in the user's database. Needed to update or delete the right row:
        /// the location's own id is a different sequence and only ever matched by coincidence.
        /// </summary>
        public int Id { get; set; }

        public string CityName { get; set; } = string.Empty;

        public string CountryName { get; set; } = string.Empty;

        public int Population { get; set; }
    }
}
