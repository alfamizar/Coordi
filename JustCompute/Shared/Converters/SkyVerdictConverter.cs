using System.Globalization;
using Compute.Core.Domain.Entities.Models;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;

namespace JustCompute.Shared.Converters
{
    /// <summary>
    /// The verdict's own colours, deliberately fixed rather than themed. Green reads as "go out"
    /// in every palette; borrowing the theme's primary would make the same sky look encouraging
    /// under one theme and discouraging under another.
    /// </summary>
    public class SkyVerdictToColorConverter : IValueConverter
    {
        // A Brush, not a Color: the implicit Border style sets Background, and in MAUI a
        // Background brush wins over BackgroundColor - binding the colour left the banner
        // wearing the theme surface with white text on it, which was unreadable.
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            new SolidColorBrush(value is SkyVerdict verdict
                ? verdict switch
                {
                    SkyVerdict.Clear => Color.FromArgb("#1B5E20"),
                    SkyVerdict.Partly => Color.FromArgb("#8D6E00"),
                    SkyVerdict.Cloudy => Color.FromArgb("#37474F"),
                    _ => Color.FromArgb("#4A4A55"),
                }
                : Color.FromArgb("#4A4A55"));

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>The verdict's headline, in the reader's language.</summary>
    public class SkyVerdictToTextConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer =
            ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            _localizer.GetString(value is SkyVerdict verdict
                ? verdict switch
                {
                    SkyVerdict.Clear => "SkyVerdictClearLabel",
                    SkyVerdict.Partly => "SkyVerdictPartlyLabel",
                    SkyVerdict.Cloudy => "SkyVerdictCloudyLabel",
                    _ => "SkyVerdictUnknownLabel",
                }
                : "SkyVerdictUnknownLabel").Value;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
