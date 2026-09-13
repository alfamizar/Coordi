using Compute.Core.Extensions;
using Compute.Core.Domain.Entities.Models.AstroSign;
using System.Collections.ObjectModel;

namespace Compute.Core.Domain.Entities.Models
{
    public class BaseCelestialBodyCycle
    {
        public static readonly IReadOnlyList<string> ZodiacSigns 
            = new ReadOnlyCollection<string>(["♈️", "♉️", "♊️", "♋️", "♌️", "♍️", "♎️", "♏️", "♐️", "♑️", "♒️", "♓️"]);

        public enum Hemisphere { Northern, Southern }

        public Hemisphere EarthHemisphere { get; set; }

        public string? ZodiacSignUnicodeIcon { get; private set; }

        public DateTime? GeoDate { get; set; }

        public DateTime? SetTime { get; set; }

        public DateTime? RiseTime { get; set; }

        public TimeSpan? Duration
        {
            get
            {
                if (SetTime != null && RiseTime != null)
                {
                    return (SetTime - RiseTime).Value.Duration().StripMilliseconds();
                }

                return TimeSpan.Zero;
            }
        }

        private AstroZodiacSign _zodiacSign;
        public AstroZodiacSign ZodiacSign
        {
            get => _zodiacSign;
            set
            {
                _zodiacSign = value;
                ZodiacSignUnicodeIcon = GlyphFor(value);
            }
        }

        /// <summary>
        /// The glyph for a sign, or an empty string when there is no sign to show.
        ///
        /// The enum starts at None = 0 and the glyph list starts at Aries, so the index is
        /// one less than the value — which for None is -1, and ElementAt(-1) throws. Nothing
        /// in the app produces None today, but a default-constructed cycle holds it, and an
        /// unhandled exception is a poor way to find that out.
        /// </summary>
        public static string GlyphFor(AstroZodiacSign sign)
        {
            int index = (int)sign - 1;
            return index >= 0 && index < ZodiacSigns.Count ? ZodiacSigns[index] : string.Empty;
        }

        public bool IsDaylightSavingTime { get; set; }
    }
}