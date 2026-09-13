using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Utils
{
    public class DistanceCalculator
    {
        private GeoPoint? _startingPoint;
        private GeoPoint? _previousPoint;
        private GeoPoint? _lastPoint;
        private DateTime? _previousTimestamp;
        private DateTime? _lastTimestamp;

        /// <summary>
        /// Altitude at the first fix of the run, or null before one has been recorded.
        ///
        /// Null rather than a sentinel: -1 used to mean "unset", which quietly zeroed the
        /// elevation for anyone starting a metre below sea level — the Dead Sea shore, Death
        /// Valley, the Caspian depression and much of the Netherlands are all legitimately
        /// negative.
        /// </summary>
        public double? StartAltitude { get; set; }

        public GeoPoint? StartingPoint => _startingPoint;

        public GeoPoint? PreviousPoint => _previousPoint;

        public GeoPoint? LastPoint => _lastPoint;

        /// <summary>When the most recent fix was taken, for callers policing plausible movement.</summary>
        public DateTime? LastFixTimestampUtc => _lastTimestamp;

        /// <summary>
        /// Records a fix and the moment the OS actually took it. Speed is derived from the
        /// interval between two fixes, so it has to be the hardware's clock: timestamping on
        /// arrival measured how long the callback took to reach the UI thread, which stretches
        /// under load and reports the traveller as slower than they were.
        /// </summary>
        public void AddFix(GeoPoint point, DateTime timestampUtc)
        {
            _startingPoint ??= point;

            _previousPoint = _lastPoint;
            _previousTimestamp = _lastTimestamp;

            _lastPoint = point;
            _lastTimestamp = timestampUtc;
        }

        /// <summary>Clears every accumulated fix so a new run starts from nothing.</summary>
        public void Reset()
        {
            StartAltitude = null;
            _startingPoint = null;
            _previousPoint = null;
            _lastPoint = null;
            _previousTimestamp = null;
            _lastTimestamp = null;
        }

        /// <summary>Height gained since the run began; zero until both altitudes are known.</summary>
        public double GetElevation(double? currentAltitude)
        {
            if (StartAltitude is not { } start || currentAltitude is not { } current)
            {
                return 0;
            }

            return current - start;
        }

        public double GetCurvedDistance(double currentCurvedDistance)
        {
            if (_startingPoint is not { } start || _lastPoint is not { } last) return 0;
            if (_previousPoint is not { } previous) return start.DistanceMetersTo(last);

            return currentCurvedDistance + previous.DistanceMetersTo(last);
        }

        public double GetDirectDistance()
        {
            if (_startingPoint is not { } start || _lastPoint is not { } last) return 0;
            return start.DistanceMetersTo(last);
        }

        public double GetSpeed()
        {
            if (_previousPoint is not { } previous || _lastPoint is not { } last ||
                _previousTimestamp == null || _lastTimestamp == null)
            {
                return 0;
            }

            double distance = previous.DistanceMetersTo(last);
            TimeSpan timeElapsed = _lastTimestamp.Value - _previousTimestamp.Value;
            return timeElapsed.TotalSeconds > 0 ? distance / timeElapsed.TotalSeconds : 0;
        }
    }
}
