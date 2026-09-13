namespace Compute.Core.Domain.Entities.Models.Fuel
{
    /// <summary>
    /// How the reader quotes fuel consumption. There is no universal figure: the same car is
    /// "7 L/100 km" in Berlin, "34 mpg" in Boston, "40 mpg" in Bristol (a bigger gallon, not a
    /// better car) and "14 km/L" in Tokyo — and two of these count upwards while two count
    /// downwards. Asking for a number without asking which of these it is gets a wrong answer
    /// that looks plausible.
    /// </summary>
    public enum FuelConsumptionMode
    {
        /// <summary>Litres per 100 km. Most of the world. Lower is better.</summary>
        LitersPer100Km,

        /// <summary>Kilometres per litre. Japan, Korea, India and others. Higher is better.</summary>
        KilometersPerLiter,

        /// <summary>Miles per US gallon (3.785 L). Higher is better.</summary>
        MilesPerUsGallon,

        /// <summary>Miles per imperial gallon (4.546 L) — the UK's, about 20% larger.</summary>
        MilesPerImperialGallon,
    }

    /// <summary>The volume a mode's answer comes back in.</summary>
    public enum FuelVolumeUnit
    {
        Liters,
        UsGallons,
        ImperialGallons,
    }
}
