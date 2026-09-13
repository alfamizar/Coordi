using Compute.Astro;
using Compute.Core.Domain.Entities.Models.Weather;

namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>How good the sky is expected to be for looking at, at a glance.</summary>
    public enum SkyVerdict
    {
        /// <summary>No forecast to judge by.</summary>
        Unknown,

        /// <summary>Clear, or near enough to be worth going out.</summary>
        Clear,

        /// <summary>Broken cloud: something will be visible, not everything.</summary>
        Partly,

        /// <summary>Overcast or worse.</summary>
        Cloudy,
    }

    /// <summary>
    /// The one-line summary of a night: is it worth going outside, and when is it actually dark.
    ///
    /// Deliberately coarse. A forecast a day out cannot honestly distinguish 60% cloud from 70%,
    /// and the reader is deciding whether to fetch a coat, not planning to the minute.
    /// </summary>
    public static class SkySummary
    {
        /// <summary>
        /// The verdict for a day's weather. Fog counts as cloudy even though it is not cloud —
        /// what matters here is whether the sky can be seen, and through fog it cannot.
        /// </summary>
        public static SkyVerdict VerdictFor(WeatherCondition condition) => condition switch
        {
            WeatherCondition.Clear => SkyVerdict.Clear,
            WeatherCondition.PartlyCloudy => SkyVerdict.Partly,
            WeatherCondition.Cloudy => SkyVerdict.Cloudy,
            WeatherCondition.Fog => SkyVerdict.Cloudy,
            WeatherCondition.Drizzle => SkyVerdict.Cloudy,
            WeatherCondition.Rain => SkyVerdict.Cloudy,
            WeatherCondition.Snow => SkyVerdict.Cloudy,
            WeatherCondition.Showers => SkyVerdict.Cloudy,
            WeatherCondition.Thunderstorm => SkyVerdict.Cloudy,
            _ => SkyVerdict.Unknown,
        };

        /// <summary>
        /// The night's astronomical darkness: last light until first light. Null when the Sun
        /// never reaches -18 degrees, which is most of a northern summer and every polar summer —
        /// the honest answer there is that the night has no astronomical darkness at all, not
        /// that it lasts zero minutes.
        /// </summary>
        public static (DateTime From, DateTime To)? DarkWindow(IReadOnlyList<TwilightStage> stages)
        {
            var lastLight = FirstOrNull(stages, TwilightLabel.LastLight);
            var firstLight = FirstOrNull(stages, TwilightLabel.FirstLight);

            if (lastLight is not { } dusk || firstLight is not { } dawn) return null;

            // The stages come from one calendar day, so first light is that morning's - it falls
            // before the evening's last light. The window a person means by "tonight" runs from
            // this evening to the next morning, and to a minute's accuracy the two mornings are
            // interchangeable, so the earlier one is carried forward a day.
            return (dusk, dawn <= dusk ? dawn.AddDays(1) : dawn);
        }

        private static DateTime? FirstOrNull(IReadOnlyList<TwilightStage> stages, TwilightLabel label)
        {
            foreach (var stage in stages)
            {
                if (stage.Label == label) return stage.Time;
            }
            return null;
        }
    }
}
