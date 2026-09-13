using System.Globalization;
using Compute.Astro;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;

namespace JustCompute.Shared.Converters
{
    /// <summary>
    /// The reader-facing name of a twilight stage. "First light" and "last light" rather than
    /// "astronomical dawn" and "astronomical dusk": the astronomical stage is the one whose name
    /// says least to someone who is not an astronomer, and those two are what it actually is.
    /// </summary>
    public class TwilightLabelToNameConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer =
            ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is TwilightLabel label
                ? _localizer.GetString(label switch
                {
                    TwilightLabel.FirstLight => "FirstLightLabel",
                    TwilightLabel.NauticalDawn => "NauticalDawnLabel",
                    TwilightLabel.CivilDawn => "CivilDawnLabel",
                    TwilightLabel.CivilDusk => "CivilDuskLabel",
                    TwilightLabel.NauticalDusk => "NauticalDuskLabel",
                    _ => "LastLightLabel",
                }).Value
                : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
