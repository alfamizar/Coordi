﻿using Compute.Core.Extensions;
using CoordinateSharp;
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

        private AstrologicalSignType _zodiacSign;
        public AstrologicalSignType ZodiacSign
        {
            get => _zodiacSign;
            set
            {
                _zodiacSign = value;
                ZodiacSignUnicodeIcon = ZodiacSigns.ElementAt((int)value - 1);
            }
        }

        public bool IsDaylightSavingTime { get; set; }
    }
}