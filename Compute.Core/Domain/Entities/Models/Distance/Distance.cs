namespace Compute.Core.Domain.Entities.Models.Distance
{
    [Serializable]
    public class Distance
    {
        private readonly double kilometers;
        private readonly double miles;
        private readonly double feet;
        private readonly double meters;
        private readonly double bearing;
        private readonly double nauticalMiles;

        public double Kilometers => kilometers;
        public double Miles => miles;
        public double NauticalMiles => nauticalMiles;
        public double Meters => meters;
        public double Feet => feet;
        public double Bearing => bearing;

        public Distance(double km)
        {
            kilometers = km;
            meters = km * 1000.0;
            feet = meters * 3.28084;
            miles = meters * 0.000621371;
            nauticalMiles = meters * 0.0005399565;
        }

        public Distance(double distance, DistanceType type)
        {
            bearing = 0.0;
            switch (type)
            {
                case DistanceType.Feets:
                    feet = distance;
                    meters = feet * 0.3048;
                    kilometers = meters / 1000.0;
                    miles = meters * 0.000621371;
                    nauticalMiles = meters * 0.0005399565;
                    break;
                case DistanceType.Kilometers:
                    kilometers = distance;
                    meters = kilometers * 1000.0;
                    feet = meters * 3.28084;
                    miles = meters * 0.000621371;
                    nauticalMiles = meters * 0.0005399565;
                    break;
                case DistanceType.Meters:
                    meters = distance;
                    kilometers = meters / 1000.0;
                    feet = meters * 3.28084;
                    miles = meters * 0.000621371;
                    nauticalMiles = meters * 0.0005399565;
                    break;
                case DistanceType.Miles:
                    miles = distance;
                    meters = miles * 1609.344;
                    feet = meters * 3.28084;
                    kilometers = meters / 1000.0;
                    nauticalMiles = meters * 0.0005399565;
                    break;
                case DistanceType.NauticalMiles:
                    nauticalMiles = distance;
                    meters = nauticalMiles * 1852.001;
                    feet = meters * 3.28084;
                    kilometers = meters / 1000.0;
                    miles = meters * 0.000621371;
                    break;
                default:
                    kilometers = distance;
                    meters = distance * 1000.0;
                    feet = meters * 3.28084;
                    miles = meters * 0.000621371;
                    nauticalMiles = meters * 0.0005399565;
                    break;
            }
        }

        public double GetByType(DistanceType type)
        {
            return type switch
            {
                DistanceType.Meters => meters,
                DistanceType.Kilometers => kilometers,
                DistanceType.Feets => feet,
                DistanceType.Miles => miles,
                DistanceType.NauticalMiles => nauticalMiles,
                _ => kilometers
            };
        }
    }
}
