using CoordinateSharp;

namespace Compute.Core.Utils
{
    public class DistanceCalculator
    {
        private double _startAltitude = -1;
        private Coordinate? _startingPoint;
        private Coordinate? _previousPoint;
        private Coordinate? _lastPoint;

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
            set => _previousPoint = value;
        }

        public Coordinate? LastPoint
        {
            get => _lastPoint;
            set => _lastPoint = value;
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
    }
}
