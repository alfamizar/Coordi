using System.Globalization;
using Compute.Core.Domain.Entities.Models.Distance;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;

namespace JustCompute.Shared.Helpers
{
    /// <summary>
    /// Metres rendered in the unit the reader chose in Settings.
    ///
    /// Shared rather than per-screen: the Ruler grew this logic first and Speed &amp; Distance
    /// never got it, so the same app measured a route in miles and the trip that drove it in
    /// metres. One implementation is the only way those two stay in agreement.
    /// </summary>
    public sealed class DistanceFormatter(IStringLocalizer<AppStringsRes> localizer)
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = localizer;

        /// <summary>The unit currently chosen, read fresh — Settings can change between screens.</summary>
        private static DistanceType Unit => Settings.DistanceType;

        /// <summary>
        /// A journey-scale distance: a route, a trip, the line between two cities.
        /// </summary>
        public string Format(double meters)
        {
            double kilometers = meters / 1000.0;

            // The abbreviations come from the resources the rest of the app uses, so "km" reads
            // as "км" in Cyrillic rather than staying Latin on this one screen.
            (double value, string suffix) = Unit switch
            {
                DistanceType.Meters => (meters, Abbreviation("MetersAbbreviationLabel")),
                DistanceType.Miles => (kilometers * 0.621371, Abbreviation("MilesAbbreviationLabel")),
                DistanceType.NauticalMiles => (kilometers * 0.539957, Abbreviation("NauticalMilesAbbreviationLabel")),
                DistanceType.Feets => (meters * 3.28084, Abbreviation("FeetsAbbreviationLabel")),
                _ => (kilometers, Abbreviation("KilometersAbbreviationLabel")),
            };

            return $"{Number(value)} {suffix}";
        }

        /// <summary>
        /// A short distance — a GPS accuracy radius, an altitude, a climb.
        ///
        /// These do not belong in the journey unit. Someone who picked kilometres so their drive
        /// reads "42 km" does not want their fix accuracy reported as "0.01 km", and nautical
        /// miles say nothing at all about how high a hill is. So the choice is read only for the
        /// system it implies, and the value is given in metres or feet.
        /// </summary>
        public string FormatShort(double meters)
        {
            bool imperial = Unit is DistanceType.Feets or DistanceType.Miles;

            return imperial
                ? $"{Number(meters * 3.28084)} {Abbreviation("FeetsAbbreviationLabel")}"
                : $"{Number(meters)} {Abbreviation("MetersAbbreviationLabel")}";
        }

        /// <summary>
        /// Enough digits to be useful and no more: below 100 the fraction still carries something,
        /// above it the reader wants a round number and grouping separators.
        /// </summary>
        public static string Number(double value) =>
            Math.Abs(value) >= 100
                ? value.ToString("N0", CultureInfo.CurrentCulture)
                : value.ToString("0.##", CultureInfo.CurrentCulture);

        private string Abbreviation(string key) => _localizer.GetString(key);
    }
}
