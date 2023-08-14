using Compute.Core.Domain.Entities.Models.AstroSign;
using System.Collections.ObjectModel;

namespace Compute.Core.Domain.Entities.Models.Moon
{
    public class MoonCycle : BaseCelestialBodyCycle
    {

        public static readonly IReadOnlyList<string> NorthernHemisphere
       = new ReadOnlyCollection<string>(new List<string> { "🌑", "🌒", "🌓", "🌔", "🌕", "🌖", "🌗", "🌘", "🌑" });

        public static readonly IReadOnlyList<string> SouthernHemisphere
            = NorthernHemisphere.Reverse().ToList().AsReadOnly();

        public string? MoonPhaseUnicodeIcon { get; private set; }

        public string? MoonInZodiacSignUnicodeIcon { get; private set; }

        public Distance.Distance? Distance { get; set; }

        public MoonName MoonName { get; set; }

        //
        // Summary:
        //     Moon's phase name enumerator for the specified day
        private MoonPhase _phaseName;
        public MoonPhase PhaseName
        {
            get => _phaseName;
            set
            {
                _phaseName = value;
                if (EarthHemisphere == Hemisphere.Northern)
                {
                    MoonPhaseUnicodeIcon = NorthernHemisphere.ElementAt((int)value);
                }
                else
                {
                    MoonPhaseUnicodeIcon = SouthernHemisphere.ElementAt((int)value);
                }
            }
        }

        private AstroZodiacSign _moonInZodiacSign;
        public AstroZodiacSign MoonInZodiacSign
        {
            get => _moonInZodiacSign;
            set
            {
                _moonInZodiacSign = value;
                MoonInZodiacSignUnicodeIcon = ZodiacSigns.ElementAt((int)value - 1);
            }
        }
    }
}
