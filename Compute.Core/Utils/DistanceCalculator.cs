using CoordinateSharp;
using System;

namespace Compute.Core.Utils
{
    public class DistanceCalculator
    {
        private double _startAltitude = -1;
        private Coordinate? _startingPoint;
        private Coordinate? _previousPoint;
        private Coordinate? _lastPoint;
        private DateTime? _previousTimestamp;
        private DateTime? _lastTimestamp;

        public double StartAltitude
        {
            get => _startAltitude;
            set => _startAltitude = value;
        }

        public Coordinate? StartingPoint
        {
            get => _startingPoint;
            set => _startingPoint = value;
        }

        public Coordinate? PreviousPoint
        {
            get => _previousPoint;
            set
            {
                _previousPoint = value;
                _previousTimestamp = DateTime.UtcNow;
            }
        }

        public Coordinate? LastPoint
        {
            get => _lastPoint;
            set
            {
                _previousTimestamp = _lastTimestamp; // Shift last timestamp to previous

                _lastPoint = value;
                _lastTimestamp = DateTime.UtcNow; // Capture current timestamp
            }
        }

        public double GetElevation(double currentAltitude)
        {
            if (_startAltitude == -1 || currentAltitude == -1)
            {
                return 0;
            }

            return currentAltitude - _startAltitude;
        }

        public double GetCurvedDistance(double currentCurvedDistance)
        {
            if (_startingPoint == null || _lastPoint == null) return 0;
            if (_previousPoint == null) return new Distance(_startingPoint, _lastPoint).Meters;

            return currentCurvedDistance + new Distance(_previousPoint, _lastPoint).Meters;
        }

        public double GetDirectDistance()
        {
            if (_startingPoint == null || _lastPoint == null) return 0;
            return new Distance(_startingPoint, _lastPoint).Meters;
        }

        public double GetSpeed()
        {
            if (_previousPoint == null || _lastPoint == null || _previousTimestamp == null || _lastTimestamp == null)
            {
                return 0;
            }

            // Calculate distance between the previous and last points in meters
            double distance = new Distance(_previousPoint, _lastPoint).Meters;

            // Calculate the time elapsed between the two points
            TimeSpan timeElapsed = _lastTimestamp.Value - _previousTimestamp.Value;

            // Calculate speed (meters per second)
            return timeElapsed.TotalSeconds > 0 ? distance / timeElapsed.TotalSeconds : 0;
        }
    }
}
